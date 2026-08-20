# CivicHero High-Level Design

## System context
```text
Citizen / Officer / Supervisor / Admin
                 |
             React SPA
                 |
      Nginx reverse proxy / TLS
        |                    |
ASP.NET Core API       SignalR hub
        |
  Application services
   |        |        |
AWS RDS   Amazon S3  Optional Gemini
 MySQL     evidence   + local fallback
```

## Main architectural decisions
- Modular monolith backend for simpler delivery and deployment.
- React single-page application with role-protected portals.
- JWT access token plus HttpOnly rotating refresh cookie.
- EF Core with Pomelo MySQL provider for AWS RDS.
- Private S3 objects accessed through authorised API operations.
- SignalR for real-time notifications with persistent inbox fallback.
- Background services for verification timeouts, rewards and AI triage.
- Nginx as the public reverse proxy for frontend, API and SignalR.

## Trust boundaries
1. Browser to Nginx over HTTPS.
2. Nginx to internal backend/front-end containers.
3. Backend to AWS RDS using TLS.
4. Backend to S3 through AWS credentials or workload identity.
5. Optional external AI calls with timeout and local fallback.

## Availability strategy
- Health endpoints for liveness and readiness.
- Retry-aware database access.
- AI and notification degradation without blocking complaint operations.
- Immutable image evidence and audit trails.
- Versioned container images with rollback procedures.
