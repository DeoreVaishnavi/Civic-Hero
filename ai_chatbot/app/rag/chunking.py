import os
from typing import List
from pathlib import Path

def chunk_text(text: str, chunk_size: int = 250, overlap: int = 40) -> List[str]:
    """
    Split text into chunks of approximately chunk_size words with overlap.

    Args:
        text: Input text to chunk
        chunk_size: Maximum number of words per chunk
        overlap: Number of words to overlap between consecutive chunks

    Returns:
        List of text chunks
    """
    if not text.strip():
        return []

    # Split text into words
    words = text.split()

    if len(words) <= chunk_size:
        return [text]

    chunks = []
    start = 0

    while start < len(words):
        # Calculate end index for this chunk
        end = start + chunk_size

        # If we're at the end, just take the remaining words
        if end >= len(words):
            chunk = " ".join(words[start:])
            chunks.append(chunk)
            break

        # Otherwise, take chunk_size words
        chunk = " ".join(words[start:end])
        chunks.append(chunk)

        # Move start forward by (chunk_size - overlap) for next chunk
        start += chunk_size - overlap

        # Ensure we don't get stuck if overlap >= chunk_size
        if start >= len(words):
            break

    return chunks

def load_knowledge_files(knowledge_base_path: str) -> List[tuple[str, str]]:
    """
    Load all text files from the knowledge base directory.

    Args:
        knowledge_base_path: Path to the knowledge base directory

    Returns:
        List of tuples (filename, content) for each text file found
    """
    knowledge_path = Path(knowledge_base_path)
    if not knowledge_path.exists():
        return []

    knowledge_files = []
    for file_path in knowledge_path.glob("*.txt"):
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
                knowledge_files.append((file_path.name, content))
        except Exception as e:
            print(f"Warning: Could not read file {file_path}: {e}")

    return knowledge_files

def chunk_knowledge_base(knowledge_base_path: str, chunk_size: int = 250, overlap: int = 40) -> List[dict]:
    """
    Load knowledge base files and chunk them.

    Args:
        knowledge_base_path: Path to the knowledge base directory
        chunk_size: Maximum number of words per chunk
        overlap: Number of words to overlap between consecutive chunks

    Returns:
        List of dictionaries with keys: 'filename', 'chunk_id', 'text'
    """
    knowledge_files = load_knowledge_files(knowledge_base_path)
    all_chunks = []

    for filename, content in knowledge_files:
        chunks = chunk_text(content, chunk_size, overlap)
        for i, chunk in enumerate(chunks):
            all_chunks.append({
                'filename': filename,
                'chunk_id': i,
                'text': chunk
            })

    return all_chunks

if __name__ == "__main__":
    # Example usage
    import sys
    sys.path.append(str(Path(__file__).parent.parent.parent))
    from ai_chatbot.app.core.config import settings

    chunks = chunk_knowledge_base(
        str(settings.KNOWLEDGE_BASE_PATH),
        settings.CHUNK_SIZE,
        settings.CHUNK_OVERLAP
    )

    print(f"Generated {len(chunks)} chunks from knowledge base")
    for i, chunk in enumerate(chunks[:3]):  # Show first 3 chunks
        print(f"\nChunk {i+1} (from {chunk['filename']}):")
        print(chunk['text'][:200] + "..." if len(chunk['text']) > 200 else chunk['text'])