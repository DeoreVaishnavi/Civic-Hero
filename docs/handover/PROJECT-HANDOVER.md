# CivicHero Project Handover

## Repository
- Backend: `backend/CivicHero.Backend`
- Frontend: `frontend/civichero-web`
- Database documentation: `database` and `docs/database`
- Deployment: `deployment`
- Automation: `scripts`
- Tests: `backend/CivicHero.Backend.Tests`, frontend tests and `tests`

## Secrets
Secrets are never included in the handover archive. Transfer them through approved AWS/GitHub/operations secret stores and rotate temporary credentials.

## Required handover evidence
- Approved release tag and commit SHA.
- Applied migration list.
- UAT results and signatures.
- Security and performance reports.
- RDS snapshot and restore evidence.
- S3 bucket policy review.
- Owner and escalation contact list.
- Known limitations and open low-severity defects.

## Acceptance
The receiving team should independently run the application, execute health checks, restore a backup in non-production and verify one complete complaint workflow before accepting ownership.
