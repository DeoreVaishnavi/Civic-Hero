from pydantic import BaseModel
from typing import List, Optional
from enum import Enum

class VerificationDecision(str, Enum):
    LIKELY_RESOLVED = "LikelyResolved"
    LIKELY_NOT_RESOLVED = "LikelyNotResolved"
    MANUAL_REVIEW_REQUIRED = "ManualReviewRequired"
    ANALYSIS_FAILED = "AnalysisFailed"

class VisionComparisonResult(BaseModel):
    decision: VerificationDecision
    confidence_score: float  # 0.0 to 1.0
    analysis_details: str
    similarity_score: float  # 0.0 to 1.0
    issue_still_visible: bool
    observations: List[str] = []

# For the actual API endpoint, we would accept file uploads
# This model represents the response structure