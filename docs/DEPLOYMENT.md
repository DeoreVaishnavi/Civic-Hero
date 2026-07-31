# CivicHero Deployment Guide

## Local production-like deployment

1. Install Docker Desktop and ensure Linux containers are enabled.
2. Apply Phase 15.
3. Run `configure-phase15-deployment.ps1 -Environment Local`.
4. Run `scripts/deployment/preflight.ps1 -Environment Local`.
5. Run `scripts/deployment/start-production-like.ps1`.
6. Open `http://localhost:5173`.

The browser reaches Nginx only. Nginx serves the React container and proxies `/api`,
`/health` and `/hubs` to the internal ASP.NET Core container.

## Production deployment

1. Create a Linux host with Docker Engine and Compose v2.
2. Copy `deployment/` and `scripts/deployment/` to the host.
3. Create `deployment/.env.production.local` from the example.
4. Create the files under `deployment/.secrets/`.
5. Add `fullchain.pem` and `privkey.pem` under `deployment/nginx/certs/`.
6. Pull immutable backend/frontend image tags.
7. Run the preflight check.
8. Create an RDS snapshot.
9. Run `remote-deploy.sh`.
10. Verify `/nginx-health`, `/health/live`, `/health/ready`, login and SignalR.

## Production command

```bash
BACKEND_IMAGE=ghcr.io/OWNER/civichero-backend:v1.0.0 \
FRONTEND_IMAGE=ghcr.io/OWNER/civichero-frontend:v1.0.0 \
bash scripts/deployment/remote-deploy.sh deployment/.env.production.local
```

## Database migrations

Do not enable automatic production migrations. Apply reviewed migrations as a separate,
controlled release step from a trusted workstation or one-off deployment job:

```powershell
cd backend/CivicHero.Backend
dotnet ef migrations list --context CivicDbContext
dotnet ef database update --context CivicDbContext
```

Create an RDS snapshot before applying schema changes.

## Monitoring profile

```bash
docker compose \
  --env-file deployment/.env.production.local \
  -f deployment/docker-compose.prod.yml \
  --profile monitoring \
  up -d
```

Grafana is bound to `127.0.0.1:3000`; access it through an SSH tunnel rather than exposing
it publicly.
