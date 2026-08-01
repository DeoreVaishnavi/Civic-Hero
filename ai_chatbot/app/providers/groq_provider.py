import os
from typing import Optional
from groq import Groq
from .base_provider import BaseLLMProvider
import logging

logger = logging.getLogger(__name__)

class GroqProvider(BaseLLMProvider):
    """
    LLM provider using Groq API.
    """

    def __init__(self):
        self.api_key = os.getenv("GROQ_API_KEY")
        self.model = os.getenv("GROQ_MODEL", "openai/gpt-oss-20b")
        self.client = None

        if self.api_key:
            try:
                self.client = Groq(api_key=self.api_key)
                logger.info(f"Groq provider initialized with model: {self.model}")
            except Exception as e:
                logger.error(f"Failed to initialize Groq client: {e}")
                self.client = None
        else:
            logger.warning("GROQ_API_KEY not set in environment variables")

    def generate_text(self, prompt: str, max_tokens: int = 1000, temperature: float = 0.7) -> str:
        """
        Generate text using Groq API.

        Args:
            prompt: The input prompt
            max_tokens: Maximum number of tokens to generate
            temperature: Sampling temperature (0.0 to 1.0)

        Returns:
            Generated text as a string

        Raises:
            Exception: If the API call fails
        """
        if not self.is_available():
            raise Exception("Groq provider is not available. Check API key.")

        try:
            chat_completion = self.client.chat.completions.create(
                messages=[
                    {
                        "role": "user",
                        "content": prompt,
                    }
                ],
                model=self.model,
                max_tokens=max_tokens,
                temperature=temperature,
            )
            return chat_completion.choices[0].message.content
        except Exception as e:
            logger.error(f"Error generating text with Groq: {e}")
            raise

    def is_available(self) -> bool:
        """
        Check if the Groq provider is available.

        Returns:
            True if the client is initialized, False otherwise
        """
        return self.client is not None
