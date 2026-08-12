# Phase 9 — AI Services

CivicHero now provides an advisory AI pipeline:

1. Complaint classification
2. Duplicate detection using location and text similarity
3. Fraud and spam risk scoring
4. Priority prediction
5. Department suggestion
6. Hotspot analysis
7. Manual review queue
8. Background triage worker

The default rule engine works offline. Gemini is optional and never becomes a hard dependency.
All AI outputs are advisory, persisted with confidence and reasoning, and subject to human review.
