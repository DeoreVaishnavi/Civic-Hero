# Phase 1 — Port and Docker Compose Fixes

This phase implements the first change requested in `changes.docx`: audit the project ports and correct the Docker Compose startup configuration.

## Standard ports

- Website / Vite / Docker public URL: `http://localhost:5173`
- Backend: `http://localhost:5180`
- Backend HTTPS profile: `https://localhost:7180`
- RabbitMQ management: `http://localhost:15672`
- MySQL / AWS RDS: `3306`
- Redis: `6379`
- RabbitMQ AMQP: `5672`

See `docs/PORTS.md` for direct-development, Docker, and test-only mappings.

## Corrections made

1. Replaced active `8088` website defaults with `5173` in Compose files, scripts, tests, and deployment documentation.
2. Corrected the Docker health override from `/api/v1/health/live` to `/health/live`.
3. Corrected the release override service from `nginx` to the actual service name `reverse-proxy`.
4. Corrected `-ForoundColor` to `-ForegroundColor` in deployment preflight.
5. Added `/health` as a liveness alias while preserving `/health/live` and `/health/ready`.
6. Updated the AWS-RDS launcher to read website and backend ports from `.env.aws-rds` rather than using hard-coded URLs.
7. Changed local deployment verification so unavailable optional dependencies do not incorrectly mark the entire website startup as failed. Strict readiness remains available through `-RequireReadiness`.
8. Corrected the monitoring startup command so it includes `docker-compose.monitoring.yml`.
9. Removed obsolete machine-specific default paths from the primary deployment scripts; they now derive the project root from their own location.
10. Added a warning when port `5173` is already occupied, especially when Vite is still running before Docker startup.

## Validation performed

- All ten Docker Compose YAML files parsed successfully.
- Primary Compose service dependencies reference existing services.
- `/health`, `/health/live`, and `/health/ready` are present in backend routing.
- No active `8088` reference remains in runtime scripts, Compose files, frontend test defaults, or deployment documentation.

Docker itself was not executed in the scan environment because the Docker CLI/engine is unavailable there. No database, migration, AWS, S3, Redis, or RabbitMQ operation was performed.
