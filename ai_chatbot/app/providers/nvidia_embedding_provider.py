import logging
from typing import List, Optional
import requests
import json
from ai_chatbot.app.core.config import settings

logger = logging.getLogger(__name__)

class NVIDIAEmbeddingProvider:
    """
    Provider for generating embeddings using NVIDIA's API.
    """

    def __init__(self):
        self.api_url = settings.NVIDIA_EMBEDDING_API_URL
        self.api_key = settings.NVIDIA_API_KEY
        self.model = settings.NVIDIA_EMBEDDING_MODEL
        self.timeout = settings.NVIDIA_API_TIMEOUT

        if not self.api_key:
            raise ValueError("NVIDIA_API_KEY must be set in environment variables")

    def generate_embeddings(self, texts: List[str], input_type: str = "passage") -> List[List[float]]:
        """
        Generate embeddings for a list of texts using NVIDIA's API.

        Args:
            texts: List of text strings to embed

        Returns:
            List of embedding vectors (each vector is a list of floats)

        Raises:
            Exception: If the API request fails
        """
        if not texts:
            return []

        # Prepare the request payload
        payload = {
            "input": texts,
            "model": self.model,
            "input_type": input_type,
            "encoding_format": "float",
            "truncate": "END",
        }

        headers = {
            "Authorization": f"Bearer {self.api_key}",
            "Content-Type": "application/json"
        }

        try:
            logger.info(f"Requesting embeddings for {len(texts)} texts from NVIDIA API")
            response = requests.post(
                self.api_url,
                headers=headers,
                data=json.dumps(payload),
                timeout=self.timeout
            )
            response.raise_for_status()

            result = response.json()
            embeddings = [item["embedding"] for item in result["data"]]
            logger.info(f"Successfully generated {len(embeddings)} embeddings")
            return embeddings

        except requests.exceptions.RequestException as e:
            logger.error(f"Failed to generate embeddings: {str(e)}")
            raise Exception(f"NVIDIA embedding API error: {str(e)}")
        except (KeyError, ValueError) as e:
            logger.error(f"Unexpected response format from NVIDIA API: {str(e)}")
            raise Exception(f"Invalid response from NVIDIA embedding API: {str(e)}")

    def generate_embedding(self, text: str) -> List[float]:
        """Generate one query embedding for semantic knowledge search."""
        embeddings = self.generate_embeddings([text], input_type="query")
        if not embeddings:
            raise ValueError("NVIDIA returned no embedding for the query.")
        return embeddings[0]

    def get_embedding_dimension(self) -> Optional[int]:
        """
        Get the dimensionality of the embeddings produced by this model.
        This might require a test call or be known from the model specification.

        Returns:
            Embedding dimension as integer, or None if unknown
        """
        # For nvidia/nv-embedqa-e5-v5, the embedding dimension is 1024
        # This is hardcoded based on the model specification
        return 1024

if __name__ == "__main__":
    # Example usage
    import sys
    import os
    sys.path.append(os.path.join(os.path.dirname(__file__), '..', '..'))

    from ai_chatbot.app.core.config import settings

    # Configure logging
    logging.basicConfig(level=logging.INFO)

    provider = NVIDIAEmbeddingProvider()
    test_texts = ["Hello world", "This is a test embedding"]
    try:
        embeddings = provider.generate_embeddings(test_texts)
        print(f"Generated {len(embeddings)} embeddings")
        print(f"Dimension of first embedding: {len(embeddings[0])}")
    except Exception as e:
        print(f"Error: {e}")
