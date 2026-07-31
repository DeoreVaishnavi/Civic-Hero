"""CLIP-backed single-image civic issue classification."""
import io
import logging
from typing import Tuple

import torch
from PIL import Image

from app.config import MODEL_NAME
from app.services.scene_comparison import scene_comparison_service

logger = logging.getLogger(__name__)

CATEGORY_LABELS = {
    "Pothole": "a photograph of a pothole or damaged road",
    "Streetlight": "a photograph of a broken street light",
    "Garbage": "a photograph of garbage, litter, or illegal dumping",
    "Drainage": "a photograph of a blocked drain or sewage overflow",
    "WaterLeak": "a photograph of a leaking municipal water pipe",
    "TrafficSignal": "a photograph of a broken traffic signal",
    "FallenTree": "a photograph of a fallen tree blocking a public area",
}


def classify_image(image_bytes: bytes) -> Tuple[str, float, list[str]]:
    service = scene_comparison_service
    if not service.open_clip_available or service.model is None or service.preprocess is None:
        return "Unknown", 0.0, ["CLIP model is unavailable; manual review is required"]

    try:
        import open_clip

        image = Image.open(io.BytesIO(image_bytes)).convert("RGB")
        image_tensor = service.preprocess(image).unsqueeze(0).to(service.device)
        labels = list(CATEGORY_LABELS)
        tokenizer = open_clip.get_tokenizer(MODEL_NAME)
        text_tokens = tokenizer([CATEGORY_LABELS[label] for label in labels]).to(service.device)

        with torch.no_grad():
            image_features = service.model.encode_image(image_tensor)
            text_features = service.model.encode_text(text_tokens)
            image_features /= image_features.norm(dim=-1, keepdim=True)
            text_features /= text_features.norm(dim=-1, keepdim=True)
            probabilities = (100.0 * image_features @ text_features.T).softmax(dim=-1)[0]

        index = int(probabilities.argmax().item())
        return labels[index], float(probabilities[index].item()), []
    except Exception as exception:
        logger.exception("CLIP category classification failed")
        return "Unknown", 0.0, [f"Model inference failed: {type(exception).__name__}"]
