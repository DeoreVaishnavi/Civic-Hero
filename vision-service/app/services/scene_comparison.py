"""
Scene comparison service using OpenCLIP for computing visual similarity between images.
"""
import torch
import torch.nn.functional as F
from PIL import Image
import io
import numpy as np
from typing import Tuple, List
import logging

logger = logging.getLogger(__name__)

# Try to import open_clip, handle gracefully if not available
try:
    import open_clip
    OPEN_CLIP_AVAILABLE = True
except ImportError:
    OPEN_CLIP_AVAILABLE = False
    logger.warning("open_clip not installed. Scene similarity features will be limited.")

class SceneComparisonService:
    def __init__(self, model_name: str = "ViT-B-32", pretrained: str = "laion2b_s34b_b79k", device: str = "cuda"):
        """
        Initialize the scene comparison service with CLIP model.

        Args:
            model_name: The CLIP model architecture to use
            pretrained: The pretrained dataset to use
            device: The device to run computations on ('cuda' or 'cpu')
        """
        self.device = device if torch.cuda.is_available() and device == "cuda" else "cpu"
        self.model = None
        self.preprocess = None

        if OPEN_CLIP_AVAILABLE:
            try:
                self.model, _, self.preprocess = open_pretrained.create_model_and_transforms(
                    model_name, pretrained=pretrained, device=self.device
                )
                self.model.eval()  # Set to evaluation mode
                logger.info(f"Loaded CLIP model {model_name} with {pretrained} on {self.device}")
            except Exception as e:
                logger.error(f"Failed to load CLIP model: {e}")
                OPEN_CLIP_AVAILABLE = False
        else:
            logger.warning("OpenCLIP not available, using fallback similarity methods")

    def _load_image(self, image_bytes: bytes) -> Image.Image:
        """
        Load an image from bytes.

        Args:
            image_bytes: Raw image bytes

        Returns:
            PIL Image object
        """
        try:
            image = Image.open(io.BytesIO(image_bytes))
            # Convert to RGB if necessary
            if image.mode != 'RGB':
                image = image.convert('RGB')
            return image
        except Exception as e:
            logger.error(f"Error loading image: {e}")
            raise ValueError(f"Invalid image data: {e}")

    def _preprocess_image(self, image: Image.Image) -> torch.Tensor:
        """
        Preprocess an image for CLIP model input.

        Args:
            image: PIL Image object

        Returns:
            Preprocessed image tensor
        """
        if self.preprocess:
            return self.preprocess(image).unsqueeze(0).to(self.device)
        else:
            # Fallback: basic preprocessing
            image = image.resize((224, 224))  # Standard CLIP input size
            image_array = np.array(image).astype(np.float32) / 255.0
            # Normalize with ImageNet mean and std
            mean = np.array([0.485, 0.456, 0.406])
            std = np.array([0.229, 0.224, 0.225])
            image_array = (image_array - mean) / std
            # Convert to tensor and rearrange dimensions to CHW
            image_tensor = torch.from_numpy(image_array).permute(2, 0, 1).unsqueeze(0)
            return image_tensor.to(self.device)

    def compute_similarity(self, image1_bytes: bytes, image2_bytes: bytes) -> float:
        """
        Compute cosine similarity between two images using CLIP embeddings.

        Args:
            image1_bytes: First image as bytes
            image2_bytes: Second image as bytes

        Returns:
            Similarity score between 0.0 and 1.0
        """
        if not OPEN_CLIP_AVAILABLE or self.model is None:
            # Fallback to basic histogram comparison if CLIP is not available
            return self._fallback_similarity(image1_bytes, image2_bytes)

        try:
            # Load and preprocess images
            image1 = self._load_image(image1_bytes)
            image2 = self._load_image(image2_bytes)

            tensor1 = self._preprocess_image(image1)
            tensor2 = self._preprocess_image(image2)

            # Get image features
            with torch.no_grad():
                features1 = self.model.encode_image(tensor1)
                features2 = self.model.encode_image(tensor2)

                # Normalize features
                features1 = F.normalize(features1, p=2, dim=1)
                features2 = F.normalize(features2, p=2, dim=1)

                # Compute cosine similarity
                similarity = torch.dot(features1.squeeze(0), features2.squeeze(0)).item()

                # Clip to [0, 1] range (CLIP similarity can be negative)
                similarity = max(0.0, min(1.0, (similarity + 1) / 2))

                return float(similarity)

        except Exception as e:
            logger.error(f"Error computing similarity: {e}")
            return self._fallback_similarity(image1_bytes, image2_bytes)

    def _fallback_similarity(self, image1_bytes: bytes, image2_bytes: bytes) -> float:
        """
        Fallback similarity method using basic image histograms when CLIP is not available.

        Args:
            image1_bytes: First image as bytes
            image2_bytes: Second image as bytes

        Returns:
            Similarity score between 0.0 and 1.0
        """
        try:
            from PIL import Image
            import cv2
            import numpy as np

            # Load images
            img1 = self._load_image(image1_bytes)
            img2 = self._load_image(image2_bytes)

            # Convert to grayscale
            gray1 = cv2.cvtColor(np.array(img1), cv2.COLOR_RGB2GRAY)
            gray2 = cv2.cvtColor(np.array(img2), cv2.COLOR_RGB2GRAY)

            # Resize to same dimensions
            height, width = min(gray1.shape[0], gray2.shape[0]), min(gray1.shape[1], gray2.shape[1])
            gray1 = cv2.resize(gray1, (width, height))
            gray2 = cv2.resize(gray2, (width, height))

            # Compute histograms
            hist1 = cv2.calcHist([gray1], [0], None, [256], [0, 256])
            hist2 = cv2.calcHist([gray2], [0], None, [256], [0, 256])

            # Normalize histograms
            cv2.normalize(hist1, hist1, alpha=0, beta=1, norm_type=cv2.NORM_MINMAX)
            cv2.normalize(hist2, hist2, alpha=0, beta=1, norm_type=cv2.NORM_MINMAX)

            # Compare histograms using correlation
            similarity = cv2.compareHist(hist1, hist2, cv2.HISTCMP_CORREL)

            # Ensure result is in [0, 1] range
            return max(0.0, min(1.0, (similarity + 1) / 2))

        except Exception as e:
            logger.error(f"Error in fallback similarity: {e}")
            # Return a moderate similarity score as fallback
            return 0.5

# Create a singleton instance
scene_comparison_service = SceneComparisonService()