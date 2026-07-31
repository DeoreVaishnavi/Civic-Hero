import re
import logging
from typing import Tuple

logger = logging.getLogger(__name__)

class PromptGuard:
    """
    A class to guard against prompt injection and other malicious inputs.
    """

    # Patterns that might indicate prompt injection attempts
    INJECTION_PATTERNS = [
        r"(?i)ignore\s+(?:previous|prior|above|all)\s+(?:instructions|rules|guidelines|prompts?)",
        r"(?i)disregard\s+(?:previous|prior|above|all)\s+(?:instructions|rules|guidelines|prompts?)",
        r"(?i)forget\s+(?:everything|all|any)\s+(?:you\s+know|you\s+were\s+told|the\s+rules)",
        r"(?i)you\s+are\s+now\s+(?:a|an)\s+",
        r"(?i)system\s*:\s*",  # Attempt to inject system message
        r"(?i)```.*?```",      # Code blocks that might contain instructions
        r"(?i)<\s*script\s*>", # Script tags
    ]

    # Patterns that might indicate attempt to extract system prompts or internal info
    INFO_EXTRACTION_PATTERNS = [
        r"(?i)reveal\s+(?:your|the)\s+(?:system\s+)?(?:prompt|instructions|rules|guidelines)",
        r"(?i)what\s+(?:are|is)\s+(?:your|the)\s+(?:system\s+)?(?:prompt|instructions|rules|guidelines)",
        r"(?i)tell\s+me\s+(?:your|the)\s+(?:system\s+)?(?:prompt|instructions|rules|guidelines)",
        r"(?i)show\s+me\s+(?:your|the)\s+(?:system\s+)?(?:prompt|instructions|rules|guidelines)",
        r"(?i)repeat\s+(?:your|the)\s+(?:previous|initial)\s+(?:message|prompt|instruction)",
        r"(?i)what\s+did\s+you\s+just\s+say",
        r"(?i)what\s+was\s+your\s+initial\s+(?:prompt|instruction)",
    ]

    # Patterns that might indicate attempt to extract API keys or secrets
    SECRET_EXTRACTION_PATTERNS = [
        r"(?i)(?:api[_\s]?key|secret|token|password|credential)s?",
        r"(?i)sk_(?:test|live)_[a-zA-Z0-9]{24,}",  # Stripe-like keys
        r"(?i)sk-[a-zA-Z0-9]{20,}",                  # OpenAI-like keys
        r"(?i)xai-[a-zA-Z0-9]{20,}",                 # xAI-like keys
        r"(?i)sk-[a-zA-Z0-9]{20,}",                  # Generic sk- pattern
        r"(?i)eyJhbGciOiJIUzI1NiIs",                # JWT token start
    ]

    @classmethod
    def sanitize_input(cls, text: str) -> str:
        """
        Sanitize user input by removing or escaping potentially harmful content.

        Args:
            text: The input text to sanitize

        Returns:
            Sanitized text
        """
        if not text:
            return ""

        # Remove any potential script tags
        text = re.sub(r"<\s*script\s*>.*?<\s*/\s*script\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential style tags that could be used for CSS injection
        text = re.sub(r"<\s*style\s*>.*?<\s*/\s*style\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential object, embed, iframe tags
        text = re.sub(r"<\s*(object|embed|iframe)[^>]*>.*?<\s*/\s*\1\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential form tags
        text = re.sub(r"<\s*form[^>]*>.*?<\s*/\s*form\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential input tags
        text = re.sub(r"<\s*input[^>]*>", "", text, flags=re.IGNORECASE)

        # Remove any potential button tags
        text = re.sub(r"<\s*button[^>]*>.*?<\s*/\s*button\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential textarea tags
        text = re.sub(r"<\s*textarea[^>]*>.*?<\s*/\s*textarea\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential select tags
        text = re.sub(r"<\s*select[^>]*>.*?<\s*/\s*select\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential meta tags
        text = re.sub(r"<\s*meta[^>]*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential link tags
        text = re.sub(r"<\s*link[^>]*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential base tags
        text = re.sub(r"<\s*base[^>]*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential title tags
        text = re.sub(r"<\s*title[^>]*>.*?<\s*/\s*title\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential svg tags
        text = re.sub(r"<\s*svg[^>]*>.*?<\s*/\s*svg\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential canvas tags
        text = re.sub(r"<\s*canvas[^>]*>.*?<\s*/\s*canvas\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential video/audio tags
        text = re.sub(r"<\s*(video|audio)[^>]*>.*?<\s*/\s*\1\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential source tags
        text = re.sub(r"<\s*source[^>]*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential track tags
        text = re.sub(r"<\s*track[^>]*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential embed tags
        text = re.sub(r"<\s*embed[^>]*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential object tags
        text = re.sub(r"<\s*object[^>]*>.*?<\s*/\s*object\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential param tags
        text = re.sub(r"<\s*param[^>]*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential area tags
        text = re.sub(r"<\s*area[^>]*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential map tags
        text = re.sub(r"<\s*map[^>]*>.*?<\s*/\s*map\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential frame tags
        text = re.sub(r"<\s*frame[^>]*>.*?<\s*/\s*frame\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential frameset tags
        text = re.sub(r"<\s*frameset[^>]*>.*?<\s*/\s*frameset\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential noframes tags
        text = re.sub(r"<\s*noframes[^>]*>.*?<\s*/\s*noframes\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential noscript tags
        text = re.sub(r"<\s*noscript[^>]*>.*?<\s*/\s*noscript\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential template tags
        text = re.sub(r"<\s*template[^>]*>.*?<\s*/\s*template\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        # Remove any potential slot tags
        text = re.sub(r"<\s*slot[^>]*>.*?<\s*/\s*slot\s*>", "", text, flags=re.IGNORECASE | re.DOTALL)

        return text

    @classmethod
    def detect_injection_attempt(cls, text: str) -> bool:
        """
        Detect if the input text contains potential prompt injection attempts.

        Args:
            text: The input text to check

        Returns:
            True if an injection attempt is detected, False otherwise
        """
        if not text:
            return False

        for pattern in cls.INJECTION_PATTERNS:
            if re.search(pattern, text):
                logging.warning(f"Potential prompt injection detected: {pattern}")
                return True

        return False

    @classmethod
    def detect_info_extraction_attempt(cls, text: str) -> bool:
        """
        Detect if the input text attempts to extract internal information.

        Args:
            text: The input text to check

        Returns:
            True if an information extraction attempt is detected, False otherwise
        """
        if not text:
            return False

        for pattern in cls.INFO_EXTRACTION_PATTERNS:
            if re.search(pattern, text):
                logging.warning(f"Potential information extraction attempt detected: {pattern}")
                return True

        return False

    @classmethod
    def detect_secret_extraction_attempt(cls, text: str) -> bool:
        """
        Detect if the input text attempts to extract secrets or API keys.

        Args:
            text: The input text to check

        Returns:
            True if a secret extraction attempt is detected, False otherwise
        """
        if not text:
            return False

        for pattern in cls.SECRET_EXTRACTION_PATTERNS:
            if re.search(pattern, text):
                logging.warning(f"Potential secret extraction attempt detected: {pattern}")
                return True

        return False

    @classmethod
    def is_safe(cls, text: str) -> Tuple[bool, str]:
        """
        Check if the input text is safe to process.

        Args:
            text: The input text to check

        Returns:
            Tuple of (is_safe, reason) where is_safe is a boolean and reason is a string
            explaining why it's unsafe (if applicable)
        """
        if not text:
            return True, "Empty input is considered safe"

        if cls.detect_injection_attempt(text):
            return False, "Potential prompt injection detected"

        if cls.detect_info_extraction_attempt(text):
            return False, "Potential information extraction attempt detected"

        if cls.detect_secret_extraction_attempt(text):
            return False, "Potential secret extraction attempt detected"

        # Additional checks could be added here (e.g., profanity, hate speech, etc.)

        return True, "Input is safe"

    @classmethod
    def sanitize_and_check(cls, text: str) -> tuple[str, bool, str]:
        """
        Sanitize the input and check for safety.

        Args:
            text: The input text to process

        Returns:
            Tuple of (sanitized_text, is_safe, reason)
        """
        sanitized = cls.sanitize_input(text)
        is_safe, reason = cls.is_safe(sanitized)
        return sanitized, is_safe, reason
