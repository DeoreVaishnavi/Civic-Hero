"""
Main FastAPI application for the AI vision microservice.
Provides endpoint for comparing complaint and resolution images to verify if issues are resolved.
"""
import logging
from typing import List
import uvicorn
from fastapi import FastAPI, File, UploadFile, HTTPException, Form
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware

from app.config import *
from app.schemas.request_models import VisionComparisonRequest
from app.schemas.response_models import VisionComparisonResult, VerificationDecision
from app.services.scene_comparison import scene_comparison_service
from app.services.issue_detection import issue_detection_service
from app.services.change_detection import change_detection_service

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize FastAPI app
app = FastAPI(
    title="CivicHero AI Vision Service",
    description="Microservice for analyzing complaint and resolution images to verify issue resolution",
    version="1.0.0"
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Configure appropriately for production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.post("/api/v1/vision/compare-resolution", response_model=VisionComparisonResult)
async def compare_resolution(
    complaint_image: bytes = File(...),
    resolution_image: bytes = File(...),
    complaint_id: int = Form(...),
    complaint_description: str = Form(None),
    complaint_latitude: float = Form(None),
    complaint_longitude: float = Form(None),
    incident_time: str = Form(None)  # ISO format string
):
    """
    Compare a complaint image with a resolution image to determine if the reported issue has been resolved.

    Args:
        complaint_image: The original complaint image file
        resolution_image: The resolution image file showing the alleged fix
        complaint_id: ID of the complaint
        complaint_description: Text description of the complaint
        complaint_latitude: Latitude of the complaint location
        complaint_longitude: Longitude of the complaint location
        incident_time: Time of the incident (ISO format string)

    Returns:
        VisionComparisonResult with decision, confidence score, and analysis details
    """
    try:
        logger.info(f"Processing comparison request for complaint {complaint_id}")

        # Validate file types (optional - can be extended)
        # For now, we'll assume the files are valid images

        # 1. Compute scene similarity using CLIP
        similarity_score = scene_comparison_service.compute_similarity(complaint_image, resolution_image)

        # 2. Detect if the specific issue is still visible in the resolution image
        issue_still_visible, issue_observations = issue_detection_service.analyze_issue_visibility(
            complaint_description or "", resolution_image
        )

        # 3. Compute change score using OpenCV
        change_score, change_observations = change_detection_service.get_change_analysis_details(
            complaint_image, resolution_image
        )

        # 4. Combine signals to make a decision
        # Logic:
        # - High similarity + issue still visible = LikelyNotResolved
        # - Low similarity + issue not visible = LikelyResolved
        # - Otherwise = ManualReviewRequired
        # - High change score suggests significant alteration

        observations = []
        observations.extend(issue_observations)
        observations.extend(change_observations)

        # Adjust similarity score based on change detection
        # If there's high change but low similarity discrepancy, weigh more on change
        adjusted_similarity = similarity_score * (1.0 - min(change_score, 0.5))  # Reduce similarity if high change

        # Determine decision based on adjusted similarity and issue visibility
        if adjusted_similarity >= SIMILARITY_THRESHOLD_HIGH and issue_still_visible:
            decision = VerificationDecision.LIKELY_NOT_RESOLVED
            confidence = min(0.9, 0.5 + (adjusted_similarity - SIMILARITY_THRESHOLD_HIGH) * 2)
        elif adjusted_similarity <= SIMILARITY_THRESHOLD_LOW and not issue_still_visible:
            decision = VerificationDecision.LIKELY_RESOLVED
            confidence = min(0.9, 0.5 + (SIMILARITY_THRESHOLD_LOW - adjusted_similarity) * 2)
        else:
            decision = VerificationDecision.MANUAL_REVIEW_REQUIRED
            confidence = 0.6

        # Boost confidence if change score is high and consistent with decision
        if decision == VerificationDecision.LIKELY_NOT_RESOLVED and change_score > 0.3:
            confidence = min(0.95, confidence + 0.1)
        elif decision == VerificationDecision.LIKELY_RESOLVED and change_score > 0.5:
            confidence = min(0.95, confidence + 0.15)

        # Ensure confidence is in valid range
        confidence = max(0.0, min(1.0, confidence))

        # Build analysis details
        analysis_details = (
            f"Similarity score: {similarity_score:.3f} (adjusted: {adjusted_similarity:.3f}), "
            f"Issue still visible: {issue_still_visible}, "
            f"Change score: {change_score:.3f}"
        )

        result = VisionComparisonResult(
            decision=decision,
            confidence_score=confidence,
            analysis_details=analysis_details,
            similarity_score=float(similarity_score),
            issue_still_visible=issue_still_visible,
            observations=observations
        )

        logger.info(f"Completed comparison for complaint {complaint_id}: {decision.value} (confidence: {confidence:.2f})")
        return result

    except Exception as e:
        logger.error(f"Error processing comparison request: {e}", exc_info=True)
        raise HTTPException(
            status_code=500,
            detail=f"Internal server error: {str(e)}"
        )

@app.get("/health")
async def health_check():
    """Health check endpoint."""
    return {"status": "healthy", "service": "vision-service"}

if __name__ == "__main__":
    uvicorn.run(
        "app.main:app",
        host=HOST,
        port=PORT,
        reload=True  # Set to False in production
    )