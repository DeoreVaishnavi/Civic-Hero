import os
from pathlib import Path
from dotenv import load_dotenv

# Load environment variables from .env file
load_dotenv()

class Settings:
    # NVIDIA API Embedding Provider
    NVIDIA_API_KEY: str = os.getenv("NVIDIA_API_KEY", "")
    NVIDIA_EMBEDDING_MODEL: str = os.getenv("NVIDIA_EMBEDDING_MODEL", "nvidia/nv-embedqa-e5-v5")
    NVIDIA_EMBEDDING_API_URL: str = os.getenv(
        "NVIDIA_EMBEDDING_API_URL",
        "https://integrate.api.nvidia.com/v1/embeddings",
    )
    NVIDIA_API_TIMEOUT: int = int(os.getenv("NVIDIA_API_TIMEOUT", "30"))

    # Application Settings
    LOG_LEVEL: str = os.getenv("LOG_LEVEL", "INFO")
    EMBEDDING_BATCH_SIZE: int = int(os.getenv("EMBEDDING_BATCH_SIZE", "32"))
    KNOWLEDGE_BASE_PATH: Path = Path(os.getenv("KNOWLEDGE_BASE_PATH", "./knowledge"))
    EMBEDDINGS_OUTPUT_PATH: Path = Path(os.getenv("EMBEDDINGS_OUTPUT_PATH", "./generated/embeddings.json"))
    CHUNK_SIZE: int = int(os.getenv("CHUNK_SIZE", "250"))
    CHUNK_OVERLAP: int = int(os.getenv("CHUNK_OVERLAP", "40"))
    TOP_K: int = int(os.getenv("TOP_K", "3"))
    MIN_SIMILARITY_SCORE: float = float(os.getenv("MIN_SIMILARITY_SCORE", "0.70"))

    # Ensure directories exist
    KNOWLEDGE_BASE_PATH.mkdir(parents=True, exist_ok=True)
    EMBEDDINGS_OUTPUT_PATH.parent.mkdir(parents=True, exist_ok=True)

settings = Settings()
