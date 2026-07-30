"""
Change detection service using OpenCV to compute pixel-level differences between images.
"""
import cv2
import numpy as np
import io
from PIL import Image
from typing import Tuple, List
import logging

logger = logging.getLogger(__name__)

class ChangeDetectionService:
    def __init__(self):
        """Initialize the change detection service."""
        pass

    def _load_image(self, image_bytes: bytes) -> np.ndarray:
        """
        Load an image from bytes as numpy array.

        Args:
            image_bytes: Raw image bytes

        Returns:
            Image as numpy array in BGR format (OpenCV default)
        """
        try:
            # Convert bytes to PIL Image
            pil_image = Image.open(io.BytesIO(image_bytes))
            # Convert to RGB if necessary
            if pil_image.mode != 'RGB':
                pil_image = pil_image.convert('RGB')
            # Convert PIL to OpenCV format (RGB to BGR)
            opencv_image = cv2.cvtColor(np.array(pil_image), cv2.COLOR_RGB2BGR)
            return opencv_image
        except Exception as e:
            logger.error(f"Error loading image: {e}")
            raise ValueError(f"Invalid image data: {e}")

    def _align_images(self, img1: np.ndarray, img2: np.ndarray) -> tuple[np.ndarray, np.ndarray]:
        """
        Align two images using feature matching (ORB feature detection).

        Args:
            img1: First image (BGR format)
            img2: Second image (BGR format)

        Returns:
            Tuple of aligned images (img1_aligned, img2_aligned)
        """
        try:
            # Convert to grayscale for feature detection
            gray1 = cv2.cvtColor(img1, cv2.COLOR_BGR2GRAY)
            gray2 = cv2.cvtColor(img2, cv2.COLOR_BGR2GRAY)

            # Initialize ORB detector
            orb = cv2.ORB_create(500)  # Detect 500 features

            # Find keypoints and descriptors
            kp1, des1 = orb.detectAndCompute(gray1, None)
            kp2, des2 = orb.detectAndCompute(gray2, None)

            # If we don't have enough features, return original images
            if des1 is None or des2 is None or len(des1) < 10 or len(des2) < 10:
                logger.warning("Not enough features found for image alignment")
                return img1, img2

            # Match features using brute force matcher
            bf = cv2.BFMatcher(cv2.NORM_HAMMING, crossCheck=True)
            matches = bf.match(des1, des2)

            # Sort matches by distance
            matches = sorted(matches, key=lambda x: x.distance)

            # Use top matches for homography
            if len(matches) < 4:
                logger.warning("Not enough good matches for homography")
                return img1, img2

            # Extract matched keypoints
            src_pts = np.float32([kp1[m.queryIdx].pt for m in matches[:10]]).reshape(-1, 1, 2)
            dst_pts = np.float32([kp2[m.trainIdx].pt for m in matches[:10]]).reshape(-1, 1, 2)

            # Find homography
            M, mask = cv2.findHomography(src_pts, dst_pts, cv2.RANSAC, 5.0)

            if M is None:
                logger.warning("Could not compute homography")
                return img1, img2

            # Warp image1 to match image2
            h, w = img2.shape[:2]
            img1_aligned = cv2.warpPerspective(img1, M, (w, h))

            return img1_aligned, img2

        except Exception as e:
            logger.error(f"Error aligning images: {e}")
            # Return original images if alignment fails
            return img1, img2

    def compute_change_score(self, image1_bytes: bytes, image2_bytes: bytes) -> float:
        """
        Compute a change score between two images using Structural Similarity Index (SSIM)
        and pixel difference metrics.

        Args:
            image1_bytes: First image (complaint) as bytes
            image2_bytes: Second image (resolution) as bytes

        Returns:
            Change score between 0.0 and 1.0 (higher means more change)
        """
        try:
            # Load images
            img1 = self._load_image(image1_bytes)
            img2 = self._load_image(image2_bytes)

            # Align images to account for slight shifts in camera position
            img1_aligned, img2_aligned = self._align_images(img1, img2)

            # Convert to grayscale for comparison
            gray1 = cv2.cvtColor(img1_aligned, cv2.COLOR_BGR2GRAY)
            gray2 = cv2.cvtColor(img2_aligned, cv2.COLOR_BGR2GRAY)

            # Ensure images are the same size
            if gray1.shape != gray2.shape:
                # Resize to match the smaller dimensions
                h = min(gray1.shape[0], gray2.shape[0])
                w = min(gray1.shape[1], gray2.shape[1])
                gray1 = cv2.resize(gray1, (w, h))
                gray2 = cv2.resize(gray2, (w, h))

            # Calculate Structural Similarity Index (SSIM)
            # Note: scikit-image has compare_ssim, but to avoid extra dependency,
            # we'll compute a simplified version using mean squared error and normalize
            mse = np.mean((gray1.astype("float") - gray2.astype("float")) ** 2)
            if mse == 0:
                # Images are identical
                return 0.0

            # Convert MSE to a similarity score (0-1 range, where 0 is identical)
            # Max possible MSE for 8-bit image is 255^2 = 65025
            max_mse = 255 ** 2
            similarity = 1.0 - (mse / max_mse)
            similarity = max(0.0, min(1.0, similarity))  # Clamp to [0,1]

            # Change score is inverse of similarity
            change_score = 1.0 - similarity

            # Additionally, compute percentage of pixels that changed significantly
            diff = cv2.absdiff(gray1, gray2)
            _, thresh = cv2.threshold(diff, 30, 255, cv2.THRESH_BINARY)
            changed_pixels = np.count_nonzero(thresh)
            total_pixels = gray1.shape[0] * gray1.shape[1]
            change_ratio = changed_pixels / total_pixels

            # Combine both metrics (weighted average)
            final_change_score = 0.7 * change_score + 0.3 * change_ratio
            final_change_score = max(0.0, min(1.0, final_change_score))

            return float(final_change_score)

        except Exception as e:
            logger.error(f"Error computing change score: {e}")
            # Return a moderate change score as fallback
            return 0.5

    def get_change_analysis_details(
        self, image1_bytes: bytes, image2_bytes: bytes
    ) -> tuple[float, list[str]]:
        """
        Get detailed change analysis including score and observations.

        Args:
            image1_bytes: First image (complaint) as bytes
            image2_bytes: Second image (resolution) as bytes

        Returns:
            Tuple of (change_score: float, observations: List[str])
        """
        try:
            change_score = self.compute_change_score(image1_bytes, image2_bytes)
            observations = []

            # Add contextual observations based on the change score
            if change_score > 0.7:
                observations.append("Significant visual changes detected between images")
            elif change_score > 0.4:
                observations.append("Moderate visual changes detected between images")
            elif change_score > 0.1:
                observations.append("Minor visual changes detected between images")
            else:
                observations.append("Very little visual change detected between images")

            # Load images to provide size information
            try:
                img1 = self._load_image(image1_bytes)
                img2 = self._load_image(image2_bytes)
                observations.append(f"Image dimensions: {img1.shape[1]}x{img1.shape[0]} -> {img2.shape[1]}x{img2.shape[0]}")
            except:
                pass

            return change_score, observations

        except Exception as e:
            logger.error(f"Error in change analysis: {e}")
            return 0.5, ["Error during change analysis"]

# Create a singleton instance
change_detection_service = ChangeDetectionService()