# CivicHero Phase 13 — Security, Reliability & API Hardening

## Objective

Phase 13 adds defence-in-depth controls before formal QA and deployment. It does not replace AWS WAF, a distributed rate limiter or a dedicated SIEM in production; it provides secure application defaults and an operations view for the current monolithic deployment.

## Implemented controls

- Per-user or per-IP fixed-window API throttling
- Stricter authentication, upload and administration policies
- `429 Too Many Requests`, `Retry-After` and rate-limit response headers
- Security headers and HSTS outside Development
- Backend server-header suppression
- Request-body and multipart upload limits
- Correlation IDs on requests and error responses
- Generic production-safe exception responses
- Current-session visibility and global session revocation
- Admin account unlock and user-session revocation
- Security operations dashboard based on immutable audit logs
- No storage of passwords, JWTs, refresh tokens or request bodies in security events

## Default limits

| Policy | Limit | Window |
|---|---:|---:|
| General API | 120 requests | 60 seconds |
| Authentication | 10 requests | 300 seconds |
| Uploads | 20 requests | 60 seconds |
| Administration | 60 requests | 60 seconds |

JSON bodies are limited to 2 MB. Complaint and resolution upload routes are limited to 30 MB.

## Endpoints

- `GET /api/v1/security/session`
- `POST /api/v1/security/sessions/revoke-all`
- `GET /api/v1/security/admin/overview`
- `GET /api/v1/security/admin/events`
- `GET /api/v1/security/admin/locked-accounts`
- `POST /api/v1/security/admin/users/{id}/unlock`
- `POST /api/v1/security/admin/users/{id}/revoke-sessions`

## Production note

The in-memory limiter is suitable for the current single backend instance. When CivicHero is horizontally scaled, move counters to Redis or enforce equivalent policies at AWS WAF/API Gateway so limits are shared across instances.
