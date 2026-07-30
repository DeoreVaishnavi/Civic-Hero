import numpy as np
from typing import List, Tuple

def cosine_similarity(vec1: List[float], vec2: List[float]) -> float:
    """
    Calculate the cosine similarity between two vectors.

    Args:
        vec1: First vector
        vec2: Second vector

    Returns:
        Cosine similarity as a float between -1 and 1
    """
    # Convert to numpy arrays for efficient computation
    a = np.array(vec1)
    b = np.array(vec2)

    # Compute dot product
    dot_product = np.dot(a, b)
    # Compute norms
    norm_a = np.linalg.norm(a)
    norm_b = np.linalg.norm(b)

    # Avoid division by zero
    if norm_a == 0 or norm_b == 0:
        return 0.0

    return dot_product / (norm_a * norm_b)

def cosine_similarity_batch(query_vector: List[float], vectors: List[List[float]]) -> List[float]:
    """
    Calculate cosine similarity between a query vector and a list of vectors.

    Args:
        query_vector: The query vector
        vectors: List of vectors to compare against

    Returns:
        List of cosine similarity scores
    """
    if not vectors:
        return []

    # Convert to numpy arrays
    query = np.array(query_vector)
    matrix = np.array(vectors)

    # Compute dot products
    dot_products = np.dot(matrix, query)
    # Compute norms
    query_norm = np.linalg.norm(query)
    matrix_norms = np.linalg.norm(matrix, axis=1)

    # Avoid division by zero
    norms_product = query_norm * matrix_norms
    norms_product[norms_product == 0] = 1e-10  # Small epsilon to prevent division by zero

    similarities = dot_products / norms_product
    return similarities.tolist()

def normalize_vector(vector: List[float]) -> List[float]:
    """
    Normalize a vector to unit length.

    Args:
        vector: Input vector

    Returns:
        Normalized vector
    """
    arr = np.array(vector)
    norm = np.linalg.norm(arr)
    if norm == 0:
        return vector
    return (arr / norm).tolist()

if __name__ == "__main__":
    # Example usage
    vec1 = [1.0, 2.0, 3.0]
    vec2 = [4.0, 5.0, 6.0]

    sim = cosine_similarity(vec1, vec2)
    print(f"Cosine similarity: {sim}")

    # Batch example
    query = [1.0, 0.0, 0.0]
    vectors = [[1.0, 0.0, 0.0], [0.0, 1.0, 0.0], [0.0, 0.0, 1.0]]
    similarities = cosine_similarity_batch(query, vectors)
    print(f"Batch similarities: {similarities}")