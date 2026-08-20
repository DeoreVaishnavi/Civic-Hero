# CivicHero Go-Live Checklist

## Quality and approval

- [ ] Phase 14 automated tests pass.
- [ ] UAT pass rate is at least 95%.
- [ ] No Critical or Major defect is open.
- [ ] Release tag and change reference are approved.

## Security

- [ ] HTTPS certificate is valid for `APP_HOST`.
- [ ] Production CORS contains only approved HTTPS origins.
- [ ] `AllowedHosts` is not `*`.
- [ ] JWT, RDS, AWS and AI secrets are externalized.
- [ ] Bootstrap SuperAdmin is disabled.
- [ ] S3 public access is blocked.
- [ ] Rate limiting and security headers are enabled.

## Data and recovery

- [ ] RDS automated backup is enabled.
- [ ] Pre-deployment RDS snapshot is available.
- [ ] Restore procedure has been tested.
- [ ] Pending migrations were reviewed and applied separately.

## Deployment

- [ ] Immutable backend and frontend images are available.
- [ ] Staging deployment passed smoke tests.
- [ ] Previous release image tags are documented.
- [ ] Production preflight passes.
- [ ] `/health/live` and `/health/ready` are healthy.
- [ ] Login, complaint submission, S3 upload and SignalR are verified.

## Operations

- [ ] Monitoring profile and alerts are active.
- [ ] Log access is restricted.
- [ ] Incident contacts and escalation path are documented.
- [ ] Citizen, officer, supervisor and admin training is complete.
- [ ] Go-live owner signs the release record.
