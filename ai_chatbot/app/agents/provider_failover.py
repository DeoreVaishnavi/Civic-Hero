import logging
from typing import Optional
from ..providers.groq_provider import GroqProvider
from ..providers.nvidia_chat_provider import NVIDIAChatProvider
from ..fallback.rule_based_fallback import RuleBasedFallbackProvider

logger = logging.getLogger(__name__)

class ProviderFailoverAgent:
    """
    Agent that manages failover between LLM providers.
    Tries Groq first, then NVIDIA NIM, then falls back to rule-based responses.
    """

    def __init__(self):
        self.providers = [
            GroqProvider(),
            NVIDIAChatProvider(),
            RuleBasedFallbackProvider()
        ]
        self.provider_names = ["Groq", "NVIDIA NIM", "Rule-based Fallback"]
        logger.info("ProviderFailoverAgent initialized with %d providers", len(self.providers))

    def generate_text(self, prompt: str, max_tokens: int = 1000, temperature: float = 0.7) -> str:
        """
        Generate text using the first available provider in the fallback chain.

        Args:
            prompt: The input prompt
            max_tokens: Maximum number of tokens to generate
            temperature: Sampling temperature (0.0 to 1.0)

        Returns:
            Generated text as a string

        Raises:
            Exception: If all providers fail
        """
        last_exception = None

        for i, (provider, name) in enumerate(zip(self.providers, self.provider_names)):
            try:
                if provider.is_available():
                    logger.info("Attempting to generate text with %s (attempt %d/%d)",
                              name, i+1, len(self.providers))
                    result = provider.generate_text(prompt, max_tokens, temperature)
                    logger.info("Successfully generated text with %s", name)
                    return result
                else:
                    logger.warning("%s provider is not available", name)
            except Exception as e:
                logger.warning("Failed to generate text with %s: %s", name, str(e))
                last_exception = e
                continue

        # If we got here, all providers failed
        error_msg = f"All LLM providers failed. Last error: {last_exception}"
        logger.error(error_msg)
        raise Exception(error_msg)

    def get_available_providers(self) -> list:
        """
        Get a list of available provider names.

        Returns:
            List of names of available providers
        """
        available = []
        for provider, name in zip(self.providers, self.provider_names):
            if provider.is_available():
                available.append(name)
        return available