import json
import math
from pathlib import Path
from typing import List, Dict, Any, Optional, Tuple
import numpy as np
from ai_chatbot.app.core.config import settings
from ai_chatbot.app.rag.cosine_similarity import cosine_similarity
import logging

logger = logging.getLogger(__name__)

class KnowledgeSearch:
    """
    Semantic search over the knowledge base using precomputed embeddings.
    """

    def __init__(self, embeddings_path: Optional[Path] = None):
        """
        Initialize the knowledge search.

        Args:
            embeddings_path: Path to the embeddings JSON file. If None, uses settings.
        """
        self.embeddings_path = embeddings_path or settings.EMBEDDINGS_OUTPUT_PATH
        self.min_similarity_score = settings.MIN_SIMILARITY_SCORE
        self.top_k = settings.TOP_K
        self.chunks: List[Dict[str, Any]] = []
        self.embeddings_matrix: Optional[np.ndarray] = None
        self._loaded = False

    def load_index(self) -> bool:
        """
        Load the embeddings index from disk.

        Returns:
            True if loaded successfully, False otherwise
        """
        try:
            if not self.embeddings_path.exists():
                logger.warning(f"Embeddings file not found: {self.embeddings_path}")
                return False

            with open(self.embeddings_path, 'r', encoding='utf-8') as f:
                data = json.load(f)

            # Extract chunks with embeddings
            self.chunks = data.get("chunks", [])

            if not self.chunks:
                logger.warning("No chunks found in embeddings file")
                return False

            # Extract embeddings into a numpy array for efficient computation
            embeddings_list = [chunk["embedding"] for chunk in self.chunks]
            self.embeddings_matrix = np.array(embeddings_list, dtype=np.float32)

            # Normalize embeddings for cosine similarity (if not already normalized)
            # We'll normalize during similarity computation if needed
            self._loaded = True
            logger.info(f"Loaded {len(self.chunks)} chunks from {self.embeddings_path}")
            return True

        except Exception as e:
            logger.error(f"Failed to load embeddings index: {str(e)}")
            self._loaded = False
            return False

    def _ensure_loaded(self) -> bool:
        """
        Ensure the index is loaded, loading it if necessary.

        Returns:
            True if loaded successfully, False otherwise
        """
        if not self._loaded:
            return self.load_index()
        return True

    def search(self, query: str, top_k: Optional[int] = None) -> List[Dict[str, Any]]:
        """
        Search the knowledge base for relevant chunks.

        Args:
            query: The search query text
            top_k: Number of results to return (defaults to settings.TOP_K)

        Returns:
            List of search results, each containing:
                - text: The text chunk
                - score: Similarity score (0-1)
                - source: Source filename
                - chunk_index: Index of chunk within source
                - chunk_id: Unique identifier for the chunk
        """
        if not self._ensure_loaded():
            logger.warning("Search index not loaded")
            return []

        if top_k is None:
            top_k = self.top_k

        # Generate embedding for the query
        # We need to use the same embedding provider as used in the index
        from ai_chatbot.app.providers.nvidia_embedding_provider import NVIDIAEmbeddingProvider
        embedding_provider = NVIDIAEmbeddingProvider()

        try:
            query_embedding = embedding_provider.generate_embedding(query)
            query_vector = np.array([query_embedding], dtype=np.float32)
        except Exception as e:
            logger.error(f"Failed to generate query embedding: {str(e)}")
            return []

        # Compute cosine similarities
        # Using numpy dot product for efficiency
        # Cosine similarity = dot(a, b) / (||a|| * ||b||)
        # If vectors are normalized, it's just dot product

        # Normalize query vector
        query_norm = np.linalg.norm(query_vector)
        if query_norm == 0:
            logger.warning("Zero-norm query vector")
            return []
        query_normalized = query_vector / query_norm

        # Normalize document vectors
        doc_norms = np.linalg.norm(self.embeddings_matrix, axis=1, keepdims=True)
        # Avoid division by zero
        doc_norms[doc_norms == 0] = 1e-10
        doc_normalized = self.embeddings_matrix / doc_norms

        # Compute similarities (dot product of normalized vectors)
        similarities = np.dot(doc_normalized, query_normalized.T).flatten()

        # Get top k indices
        top_indices = np.argsort(similarities)[::-1][:top_k]

        # Build results list
        results = []
        for idx in top_indices:
            score = float(similarities[idx])
            if score < self.min_similarity_score:
                # Skip results below minimum similarity threshold
                continue

            chunk = self.chunks[idx]
            result = {
                "text": chunk["text"],
                "score": score,
                "source": chunk["source"],
                "chunk_index": chunk["chunk_index"],
                "chunk_id": chunk["chunk_id"]
            }
            results.append(result)

        logger.info(f"Found {len(results)} results for query (min_similarity={self.min_similarity_score})")
        return results

    def search_by_embedding(self, query_embedding: List[float], top_k: Optional[int] = None) -> List[Dict[str, Any]]:
        """
        Search using a precomputed query embedding.

        Args:
            query_embedding: Precomputed embedding vector for the query
            top_k: Number of results to return

        Returns:
            List of search results
        """
        if not self._ensure_loaded():
            logger.warning("Search index not loaded")
            return []

        if top_k is None:
            top_k = self.top_k

        query_vector = np.array([query_embedding], dtype=np.float32)

        # Normalize query vector
        query_norm = np.linalg.norm(query_vector)
        if query_norm == 0:
            logger.warning("Zero-norm query vector")
            return []
        query_normalized = query_vector / query_norm

        # Normalize document vectors
        doc_norms = np.linalg.norm(self.embeddings_matrix, axis=1, keepdims=True)
        doc_norms[doc_norms == 0] = 1e-10
        doc_normalized = self.embeddings_matrix / doc_norms

        # Compute similarities
        similarities = np.dot(doc_normalized, query_normalized.T).flatten()

        # Get top k indices
        top_indices = np.argsort(similarities)[::-1][:top_k]

        # Build results list
        results = []
        for idx in top_indices:
            score = float(similarities[idx])
            if score < self.min_similarity_score:
                continue

            chunk = self.chunks[idx]
            result = {
                "text": chunk["text"],
                "score": score,
                "source": chunk["source"],
                "chunk_index": chunk["chunk_index"],
                "chunk_id": chunk["chunk_id"]
            }
            results.append(result)

        return results

    def get_chunk_by_id(self, chunk_id: str) -> Optional[Dict[str, Any]]:
        """
        Retrieve a specific chunk by its ID.

        Args:
            chunk_id: The chunk identifier

        Returns:
            The chunk data if found, None otherwise
        """
        if not self.driver_loaded():
            return None

        for chunk in self.chunks:
            if chunk.get("chunk_id") == chunk_id:
                return chunk
        return None

def main():
    """
    Example usage of the KnowledgeSearch class.
    """
    # Configure logging
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
    )

    # Initialize search
    searcher = KnowledgeSearch()

    # Example search
    if searcher._ensure_loaded():
        query = "How do I report a pothole on a city street?"
        results = searcher.search(query, top_k=3)

        print(f"\nSearch results for: '{query}'")
        print("-" * 50)
        for i, result in enumerate(results, 1):
            print(f"{i}. Score: {result['score']:.4f}")
            print(f"   Source: {result['source']} (chunk {result['chunk_index']})")
            print(f"   Text: {result['text'][:150]}...")
            print()
    else:
        print("Failed to load search index. Please run the index builder first.")

if __name__ == "__main__":
    main()