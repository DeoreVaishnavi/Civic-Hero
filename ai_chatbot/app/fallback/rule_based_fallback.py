import re
import random
class RuleBasedFallbackProvider:
    """Provides the rule-based fallback LLM provider.
    """

    def __init__(self):
        # Predefined responses for common civic issues
        self.responses = {
            "pothole": [
                "To report a pothole, please contact the Public Works Department at (555) 123-4567 or use the online portal at city.gov/pothole-report.",
                "Pothole reports should be directed to the Public Works Department. You can submit a request online or call the maintenance hotline.",
                "For pothole repairs, contact the Street Maintenance Division of Public Works. They typically respond within 3 business days."
            ],
            "street light": [
                "To report a street light outage, contact the Public Works Department's Street Lighting Division.",
                "Street light issues should be reported to the Public Works Department. You can submit a request online or call the maintenance hotline.",
                "For street light repairs, contact the Street Maintenance Division of Public Works. They typically respond within 2 business days."
            ],
            "garbage": [
                "For garbage collection issues, contact the Public Works Department's Solid Waste Division.",
                "Garbage collection problems should be reported to the Public Works Department. You can schedule a pickup online or call the sanitation hotline.",
                "For missed garbage collection, contact the Solid Waste Division of Public Works. They typically return within 1 business day to collect missed items."
            ],
            "water": [
                "For water-related issues (leaks, pressure problems, etc.), contact the Public Works Department's Water Utilities Division.",
                "Water service problems should be reported to the Public Works Department. For emergencies, call the 24-hour water emergency line.",
                "For non-emergency water issues, contact the Water Utilities Division of Public Works. They typically respond within 4 business hours."
            ],
            "street": [
                "For street maintenance issues (potholes, cracks, debris), contact the Public Works Department's Street Maintenance Division.",
                "Street maintenance problems should be reported to the Public Works Department. You can submit a request online or call the maintenance hotline.",
                "For general street maintenance, contact the Street Maintenance Division of Public Works. They typically respond within 3 business days."
            ],
            "default": [
                "Thank you for contacting CivicHero. For non-emergency municipal services, please visit our website at city.gov/services or call the citizen service center at (555) 123-4567.",
                "Your concern has been received. For general municipal service requests, please visit our website or contact the citizen service center during business hours.",
                "Thank you for reaching out to CivicHero. For assistance with city services, please visit our website or call 311 for non-emergency municipal services."
            ]
        }

    def _match_category(self, prompt: str) -> str:
        """
        Match the prompt to a response category based on keywords.

        Args:
            prompt: The input prompt

        Returns:
            The matched category key
        """
        prompt_lower = prompt.lower()

        # Define keywords for each category
        keywords = {
            "pothole": ["pothole", "street hole", "road hole", "pavement hole"],
            "street light": ["street light", "streetlight", "light out", "lamp post", "traffic light"],
            "garbage": ["garbage", "trash", "rubbish", "waste collection", "sanitation"],
            "water": ["water leak", "water pressure", "broken pipe", "flooding", "sewer"],
            "street": ["street", "road", "pavement", "sidewalk", "curb"]
        }

        # Check each category for keyword matches
        for category, words in keywords.items():
            for word in words:
                if word in prompt_lower:
                    return category

        # Return default if no match found
        return "default"

    def generate_text(self, prompt: str, max_tokens: int = 1000, temperature: float = 0.7) -> str:
        """
        Generate text using rule-based responses.

        Args:
            prompt: The input prompt
            max_tokens: Maximum number of tokens to generate (ignored in rule-based)
            temperature: Sampling temperature (ignored in rule-based)

        Returns:
            Generated text as a string
        """
        # Match the prompt to a category
        category = self._match_category(prompt)

        # Get a random response from the matching category
        responses = self.responses.get(category, self.responses["default"])
        response = random.choice(responses)

        return response

    def is_available(self) -> bool:
        """
        Check if the rule-based fallback provider is available.

        Returns:
            Always True for rule-based provider
        """
        return True
