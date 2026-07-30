from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from ai_chatbot.app.api.health_routes import router as health_router
from ai_chatbot.app.api.chat_routes import router as chat_router
from ai_chatbot.app.core.logging_config import logger
import uvicorn

# Create FastAPI app
app = FastAPI(
    title="CivicHero AI Chatbot",
    description="AI-powered chatbot for civic engagement and municipal services",
    version="1.0.0",
    docs_url="/docs",
    redoc_url="/redoc"
)

# Configure CORS
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # In production, restrict to specific origins
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Include routers
app.include_router(health_router)
app.include_router(chat_router)

@app.on_event("startup")
async def startup_event():
    """Application startup event"""
    logger.info("Starting CivicHero AI Chatbot service...")
    logger.info(f"Docs available at: http://localhost:8001/docs")

@app.on_event("shutdown")
async def shutdown_event():
    """Application shutdown event"""
    logger.info("Shutting down CivicHero AI Chatbot service...")

# Health check endpoint at root level
@app.get("/")
async def root():
    return {
        "message": "Welcome to CivicHero AI Chatbot API",
        "version": "1.0.0",
        "docs": "/docs",
        "health": "/health"
    }

if __name__ == "__main__":
    uvicorn.run(
        "app.main:app",
        host="0.0.0.0",
        port=8001,
        reload=True,
        log_level="info"
    )