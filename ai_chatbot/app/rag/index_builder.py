import json
import os
from pathlib import Path
from typing import List, Dict, Any
from ai_chatbot.app.rag.chunking import chunk_text
from ai_chatbot.app.providers.nvidia_embedding_provider import NVIDIAEmbeddingProvider
from ai_chatbot.app.core.config import settings
import logging

logger = logging.getLogger(__name__)

class KnowledgeIndexBuilder:
    """
    Builds a searchable index of knowledge base embeddings.
    """

    def __init__(self):
        self.knowledge_path = settings.KNOWLEDGE_BASE_PATH
        self.output_path = settings.EMBEDDINGS_OUTPUT_PATH
        self.chunk_size = settings.CHUNK_SIZE
        self.chunk_overlap = settings.CHUNK_OVERLAP
        self.embedding_provider = NVIDIAEmbeddingProvider()
        self.min_similarity_score = settings.MIN_SIMILARITY_SCORE

        # Ensure output directory exists
        self.output_path.parent.mkdir(parents=True, exist_ok=True)

    def load_knowledge_files(self) -> List[Dict[str, str]]:
        """
        Load all text files from the knowledge base directory.

        Returns:
            List of dictionaries containing file content and metadata
        """
        knowledge_items = []

        if not self.knowledge_path.exists():
            logger.warning(f"Knowledge base path does not exist: {self.knowledge_path}")
            return knowledge_items

        # Look for .txt files
        for file_path in self.knowledge_path.glob("*.txt"):
            try:
                with open(file_path, 'r', encoding='utf-8') as f:
                    content = f.read().strip()

                if content:
                    knowledge_items.append({
                        "content": content,
                        "source": file_path.name,
                        "path": str(file_path)
                    })
                    logger.info(f"Loaded knowledge file: {file_path.name} ({len(content)} characters)")
                else:
                    logger.warning(f"Empty file skipped: {file_path.name}")

            except Exception as e:
                logger.error(f"Error reading file {file_path}: {str(e)}")

        return knowledge_items

    def build_index(self) -> Dict[str, Any]:
        """
        Build the search index by chunking knowledge files and generating embeddings.

        Returns:
            Dictionary containing the index data
        """
        logger.info("Starting knowledge index build process")

        # Load knowledge files
        knowledge_items = self.load_knowledge_files()
        if not knowledge_items:
            logger.warning("No knowledge files found to index")
            return {"chunks": [], "embeddings": []}

        # Process each knowledge item
        all_chunks = []
        all_embeddings = []

        for item in knowledge_items:
            # Chunk the text
            chunks = chunk_text(
                item["content"],
                self.chunk_size,
                self.chunk_overlap
            )

            # Add metadata to each chunk
            for i, chunk_text in enumerate(chunks):
                chunk_data = {
                    "text": chunk_text,
                    "source": item["source"],
                    "chunk_index": i,
                    "chunk_id": f"{item['source']}_chunk_{i}"
                }
                all_chunks.append(chunk_data)

            logger.info(f"Created {len(chunks)} chunks from {item['source']}")

        # Generate embeddings for all chunks
        if all_chunks:
            texts_to_embed = [chunk["text"] for chunk in all_chunks]
            logger.info(f"Generating embeddings for {len(texts_to_embed)} chunks")

            try:
                embeddings = self.embedding_provider.generate_embeddings(texts_to_embed)
                logger.info(f"Successfully generated {len(embeddings)} embeddings")

                # Combine chunks with their embeddings
                indexed_chunks = []
                for i, (chunk, embedding) in enumerate(zip(all_chunks, embeddings)):
                    indexed_chunk = chunk.copy()
                    indexed_chunk["embedding"] = embedding
                    indexed_chunks.append(indexed_chunk)

                # Create the final index structure
                index_data = {
                    "model": self.embedding_provider.model,
                    "chunk_size": self.chunk_size,
                    "chunk_overlap": self.chunk_overlap,
                    "total_chunks": len(indexed_chunks),
                    "chunks": indexed_chunks
                }

                return index_data

            except Exception as e:
                logger.error(f"Failed to generate embeddings: {str(e)}")
                raise
        else:
            logger.warning("No chunks to embed")
            return {"chunks": [], "embeddings": []}

    def save_index(self, index_data: Dict[str, Any]) -> None:
        """
        Save the index data to JSON file.

        Args:
            index_data: Dictionary containing the index data
        """
        try:
            with open(self.output_path, 'w', encoding='utf-8') as f:
                json.dump(index_data, f, indent=2)
            logger.info(f"Index saved to {self.output_path}")
        except Exception as e:
            logger.error(f"Failed to save index: {str(e)}")
            raise

    def build_and_save(self) -> None:
        """
        Build the index and save it to file.
        """
        index_data = self.build_index()
        self.save_index(index_data)

def main():
    """
    Main function to run the index builder from command line.
    """
    # Configure logging
    logging.basicConfig(
        level=logging.INFO,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
    )

    builder = KnowledgeIndexBuilder()
    try:
        builder.build_and_save()
        print(f"Successfully built knowledge index at {builder.output_path}")
    except Exception as e:
        logger.error(f"Failed to build index: {str(e)}")
        raise

if __name__ == "__main__":
    main()