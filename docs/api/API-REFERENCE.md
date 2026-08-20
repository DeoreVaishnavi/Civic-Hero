# CivicHero API Reference

## Base paths
- Local backend: `http://localhost:5180/api/v1`
- Production-like Nginx: `http://localhost:5173/api/v1`

## Authentication
- `POST /auth/register`
- `POST /auth/verify-email`
- `POST /auth/login`
- `POST /auth/refresh-token`
- `POST /auth/logout`
- `GET /auth/me`

## Major resources
- Users: `/users`
- Complaints: `/complaints`
- Assignments: `/assignments`
- Verifications: `/verifications`
- Disputes: `/disputes`
- Notifications: `/notifications`
- Rewards: `/rewards`
- AI: `/ai`
- Analytics: `/analytics`
- Chatbot: `/chatbot`
- Administration: `/admin`
- Security: `/security`
- Quality: `/quality`
- Release: `/release`
- Launch and handover: `/launch`

## Health
- `GET /health/live`
- `GET /health/ready`

## Phase 16 endpoints
- `GET /api/v1/launch/readiness`
- `GET /api/v1/launch/document-catalog`

Both Phase 16 endpoints require Admin or SuperAdmin authentication. Readiness reports artifact presence and evidence requirements; it does not claim UAT or stakeholder approval.
