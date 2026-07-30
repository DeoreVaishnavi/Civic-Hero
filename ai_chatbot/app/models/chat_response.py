from pydantic import BaseModel
from typing import Optional, List
from datetime import datetime

class ChatResponse(BaseModel):
    """Response model for chat endpoint"""
    message: str = Field(..., description="Assistant's response message")
    conversation_id: str = Field(..., description="Conversation ID for tracking")
    user_id: Optional[str] = Field(None, description="User identifier")
    timestamp: datetime = Field(default_factory=datetime.utcnow, description="Response timestamp")
    sources: Optional[List[dict]] = Field(None, description="Knowledge sources used for the response")
    confidence: Optional[float] = Field(None, ge=0.0, le=1.0, description="Confidence score of the response")

    class Config:
        schema_extra = {
            "example": {
                "message": "To report a pothole on Main Street, please contact the Public Works Department's Street Maintenance Division at (555) 123-4567 or use the online portal at city.gov/pothole-report. You can also submit a request through the city's mobile app.",
                "conversation_id": "conv_123",
                "user_id": "user_456",
                "timestamp": "2023-01-15T10:30:00Z",
                "sources": [
                    {
                        "text": "For pothole repairs, contact the Street Maintenance Division of Public Works. They typically respond within 3 business days.",
                        "source": "public_works_procedures.txt",
                        "score": 0.85
                    }
                ],
                "confidence": 0.92
            }
        }