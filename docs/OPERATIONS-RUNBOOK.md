# CivicHero Operations Runbook

## Daily checks

- `/health/live` returns HTTP 200.
- `/health/ready` returns HTTP 200 and RDS/S3 checks are healthy.
- Nginx, backend and frontend containers are healthy.
- No repeated authentication or rate-limit anomalies appear in the Security Centre.
- Background workers are processing notifications, rewards, AI triage and verification timeouts.

## Useful commands

```bash
docker compose --env-file deployment/.env.production.local -f deployment/docker-compose.prod.yml ps
docker compose --env-file deployment/.env.production.local -f deployment/docker-compose.prod.yml logs --tail=200 backend
docker compose --env-file deployment/.env.production.local -f deployment/docker-compose.prod.yml restart backend
```

## Incident severity

- **Critical:** full outage, database unavailable, suspected data breach.
- **High:** complaint submission, authentication or officer workflow unavailable.
- **Medium:** delayed notifications, AI fallback, analytics/report issue.
- **Low:** cosmetic or non-blocking issue.

## Critical incident flow

1. Record start time and affected functions.
2. Stop deployment changes.
3. Check edge, frontend, backend, RDS and S3 health in that order.
4. Preserve logs and correlation IDs.
5. Roll back application images when the incident follows a release.
6. Restore data only after confirming data corruption and obtaining approval.
7. Complete a post-incident review with root cause and preventive actions.
