from fastapi import APIRouter, HTTPException, status, Header, Depends
from ai_chatbot.app.models.chat_request import ChatRequest
from ai_chatbot.app.models.chat_response import ChatResponse
from ai_chatbot.app.agents.provider_failover import ProviderFailoverAgent
from ai_chatbot.app.rag.knowledge_search import KnowledgeSearch
from ai_chatbot.app.safety.prompt_guard import PromptGuard
import logging
import hmac
import os
import uuid
from datetime import datetime

logger = logging.getLogger(__name__)

# Initialize components
failover_agent = ProviderFailoverAgent()
knowledge_search = KnowledgeSearch()
prompt_guard = PromptGuard()

# Router
router = APIRouter(
    prefix="/api/v1/chat",
    tags=["chat"]
)

# Dependency to verify internal service key
async def verify_internal_service_key(x_internal_service_key: str = Header(None)):
    """Verify the internal service key for microservice communication"""
    configured_key = os.getenv("INTERNAL_SERVICE_KEY", "").strip()
    if not configured_key:
        logger.error("INTERNAL_SERVICE_KEY is not configured")
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="Internal service authentication is not configured"
        )

    if not x_internal_service_key:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Missing X-Internal-Service-Key header"
        )

    if not hmac.compare_digest(x_internal_service_key.strip(), configured_key):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid X-Internal-Service-Key"
        )

    return x_internal_service_key

@router.post("/generate", response_model=ChatResponse)
async def generate_chat_response(
    chat_request: ChatRequest,
    internal_service_key: str = Depends(verify_internal_service_key)
):
    """
    Generate a chat response using the AI pipeline with RAG capabilities.

    This endpoint is protected by the X-Internal-Service-Key header and should
    only be called by trusted internal services (like the ASP.NET Core backend).
    """
    try:
        # Extract request parameters
        user_message = chat_request.message
        conversation_id = chat_request.conversation_id or str(uuid.uuid4())
        user_id = chat_request.user_id
        include_context = chat_request.include_context

        logger.info(f"Processing chat request for conversation {conversation_id}")

        # 1. Input validation and sanitization
        sanitized_message, is_safe, safety_reason = prompt_guard.sanitize_and_check(user_message)
        if not is_safe:
            logger.warning(f"Unsafe input detected: {safety_reason}")
            raise HTTPException(
                status_code=status.HTTP_400_BAD_REQUEST,
                detail=f"Invalid input: {safety_reason}"
            )

        # 2. Retrieve relevant context if requested
        context_sources = []
        context_text = ""

        if include_context:
            try:
                # Search for relevant knowledge
                search_results = knowledge_search.search(sanitized_message, top_k=3)

                if search_results:
                    context_sources = [
                        {
                            "source": result.get("source", "knowledge-base"),
                            "text": result.get("text", ""),
                            "score": result.get("score")
                        }
                        for result in search_results
                    ]
                    context_text = "\n\nRelevant information:\n" + "\n---\n".join([
                        f"Source: {result['source']}\n{result['text']}"
                        for result in search_results
                    ])
                    logger.info(f"Retrieved {len(search_results)} relevant knowledge chunks")
                else:
                    logger.info("No relevant knowledge found for query")

            except Exception as e:
                logger.warning(f"Error retrieving knowledge: {e}")
                # Continue without context if search fails

        # 3. Construct the prompt with context
        system_prompt = """You are CivicHero, an AI assistant designed to help citizens with municipal services and civic engagement. Your primary goal is to provide helpful, accurate, and actionable information about local government services, complaint procedures, and civic processes.

Key responsibilities:
- Provide clear guidance on how to report civic issues (potholes, street lights, garbage, water issues, etc.)
- Explain municipal processes and procedures in simple terms
- Direct users to the appropriate city departments or officials
- Help users understand their rights and responsibilities as citizens
- Offer information about public meetings, events, and participation opportunities
- Assist with navigating city websites and online portals
- Explain local ordinances, permits, and regulations when relevant

Guidelines:
- Always be polite, professional, and empathetic
- Provide specific, actionable steps when possible
- If you don't know specific contact information or procedures, be honest and suggest official sources
- Never make up phone numbers, email addresses, or website URLs
- If a question is outside your scope, politely redirect to appropriate resources
- For emergency situations, always advise calling 911
- Keep responses concise but thorough
- Use a helpful, friendly tone appropriate for a public service assistant

When users ask about specific issues:
1. Identify the type of issue (pothole, noise, trash, etc.)
2. Determine the appropriate department to contact
3. Provide contact information if known from your knowledge base
4. Suggest both online and phone/email options when available
5. Mention any relevant forms, portals, or procedures
6. Note typical response times or processes if known from your knowledge base

If you need to consult your knowledge base for specific information, indicate that you're looking up relevant details."""

        # Combine system prompt, context, and user message
        full_prompt = f"{system_prompt}{context_text}\n\nUser message: {sanitized_message}\n\nAssistant:"

        # 4. Generate response using the fallback agent
        try:
            ai_response = failover_agent.generate_text(
                prompt=full_prompt,
                max_tokens=1000,
                temperature=0.7
            )

            # Determine which provider was used (simplified)
            provider_used = "Unknown"
            if failover_agent.providers[0].is_available():
                provider_used = "Groq"
            elif failover_agent.providers[1].is_available():
                provider_used = "NVIDIA NIM"
            else:
                provider_used = "Rule-based Fallback"

        except Exception as e:
            logger.error(f"All LLM providers failed: {e}")
            # Fallback response
            ai_response = "I apologize, but I'm experiencing technical difficulties. Please try again later or contact the city directly for assistance."
            provider_used = "Emergency Fallback"

        # 5. Create and return response
        response = ChatResponse(
            message=ai_response,
            conversation_id=conversation_id,
            message_id=str(uuid.uuid4()),
            timestamp=datetime.utcnow(),
            provider_used=provider_used,
            confidence_score=0.85 if provider_used not in ["Rule-based Fallback", "Emergency Fallback"] else 0.6,
            sources=context_sources if context_sources else None
        )

        logger.info(f"Generated response for conversation {conversation_id} using {provider_used}")
        return response

    except HTTPException:
        raise
    except Exception as e:
        logger.error(f"Unexpected error in chat endpoint: {e}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Internal server error occurred while processing your request"
        )
