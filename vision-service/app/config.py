# Configuration for the vision microservice
import os
from dotenv import load_dotenv

# Load environment variables from .env file
load_dotenv()

# Model configuration
MODEL_NAME = os.getenv("CLIP_MODEL", "ViT-B-32")
PRETRAINED_DATASET = os.getenv("PRETRAINED_DATASET", "laion2b_s34b_b79k")
DEVICE = os.getenv("DEVICE", "cuda")  # Will fallback to cuda if available, else cpu

# Similarity thresholds
SIMILARITY_THRESHOLD_HIGH = float(os.getenv("SIMILARITY_THRESHOLD_HIGH", "0.8"))
SIMILARITY_THRESHOLD_LOW = float(os.getenv("SIMILARITY_THRESHOLD_LOW", "0.4"))

# Confidence thresholds
CONFIDENCE_THRESHOLD = float(os.getenv("CONFIDENCE_THRESHOLD", "0.65"))

# File upload settings
MAX_FILE_SIZE = int(os.getenv("MAX_FILE_SIZE", "10485760"))  # 10MB
ALLOWED_EXTENSIONS = {"png", "jpg", "jpeg", "gif", "bmp", "tiff"}

# Server settings
HOST = os.getenv("HOST", "0.0.0.0")
PORT = int(os.getenv("PORT", "8001"))