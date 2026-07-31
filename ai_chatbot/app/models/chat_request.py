from pydantic import BaseModel, Field
from typing import Optional

class ChatRequest(BaseModel):
    """Request model for chat endpoint"""
    message: str = Field(..., min_length=1, max_length=2000, description="User message")
    conversation_id: Optional[str] = Field(None, description="Conversation ID for tracking")
    user_id: Optional[str] = Field(None, description="User identifier")
    include_context: bool = Field(True, description="Whether to include contextual information")

    class Config:
        json_schema_extra = {
            "example": {
                "message": "How do I report a pothole on Main Street?",
                "conversation_id": "conv_123",
                "user_id": "user_456",
                "include_context": True
            }
        }
