from fastapi import APIRouter
from datetime import datetime

router = APIRouter(
    prefix="/health",
    tags=["health"],
)

@router.get("")
async def health_check():
    """
    Health check endpoint to verify the service is running.
    """
    return {
        "status": "healthy",
        "timestamp": datetime.utcnow().isoformat(),
        "service": "CivicHero AI Chatbot",
        "version": "1.0.0"
    }

@router.get("/ping")
async def ping():
    """
    Simple ping endpoint.
    """
    return {"message": "pong"}