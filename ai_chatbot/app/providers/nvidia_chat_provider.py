import os
from typing import Optional
from openai import OpenAI
from .base_provider import BaseLLMProvider
import logging

logger = logging.getLogger(__name__)

class NVIDIAChatProvider(BaseLLMProvider):
    """
    LLM provider using NVIDIA NIM API (OpenAI compatible).
    """

    def __init__(self):
        self.api_key = os.getenv("NVIDIA_API_KEY")
        self.base_url = os.getenv("NVIDIA_BASE_URL", "https://integrate.api.nvidia.com/v1")
        self.model = os.getenv("NVIDIA_CHAT_MODEL", "meta/llama-3.1-8b-instruct")
        self.client = None

        if self.api_key:
            try:
                self.client = OpenAI(
                    base_url=self.base_url,
                    api_key=self.api_key
                )
                logger.info(f"NVIDIA Chat provider initialized with model: {self.model} at {self.base_url}")
            except Exception as e:
                logger.error(f"Failed to initialize NVIDIA client: {e}")
                self.client = None
        else:
            logger.warning("NVIDIA_API_KEY not set in environment variables")

    def generate_text(self, prompt: str, max_tokens: int = 1000, temperature: float = 0.7) -> str:
        """
        Generate text using NVIDIA NIM API.

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
            raise Exception("NVIDIA Chat provider is not available. Check API key.")

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
            logger.error(f"Error generating text with NVIDIA: {e}")
            raise

    def is_available(self) -> bool:
        """
        Check if the NVIDIA Chat provider is available.

        Returns:
            True if the client is initialized, False otherwise
        """
        return self.client is not None
