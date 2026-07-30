from pydantic import BaseModel
from typing import List, Optional

class VisionComparisonRequest(BaseModel):
    complaint_id: int
    complaint_description: Optional[str] = None
    # Note: In a real implementation, the image bytes would be sent via multipart/form-data
    # For simplicity in this API contract, we'll assume they're handled separately
    # or passed as base64 encoded strings. For production, consider using file uploads.

class VisionComparisonResult(BaseModel):
    decision: str  # "LikelyResolved", "LikelyNotResolved", "ManualReviewRequired", "AnalysisFailed"
    confidence_score: float  # 0.0 to 1.0
    analysis_details: str
    similarity_score: float  # 0.0 to 1.0
    issue_still_visible: bool
    observations: List[str] = []

# For actual file upload, we would use:
# From fastapi import File, UploadFile
# But for the request model, we keep it simple