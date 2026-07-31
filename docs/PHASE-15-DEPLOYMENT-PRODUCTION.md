# Phase 15 — Deployment and Production Readiness

Phase 15 converts the Phase 14 release candidate into a deployable, observable and
recoverable application. It does not add a business database migration.

## Included

- Multi-stage ASP.NET Core 8 and React container builds
- Non-root application containers
- Local production-like and production Compose definitions
- Nginx routing for the SPA, `/api`, `/health` and SignalR `/hubs`
- External Docker secret files
- GitHub Actions build, container publishing and gated deployments
- Health probing with Prometheus Blackbox Exporter and Grafana
- Preflight, verification, RDS snapshot, remote deployment and rollback scripts
- Admin Release Centre at `/admin/release-center`

## Release principle

A configured deployment is not automatically approved. Production approval also requires:

1. Phase 14 quality gates and UAT sign-off.
2. A current RDS snapshot.
3. Reviewed environment and secret configuration.
4. Valid TLS certificates.
5. Successful staging deployment and smoke test.
6. A documented rollback image tag.
