# CivicHero Maintenance Plan

## Daily
- Review health, error logs and security events.
- Review critical and overdue complaints.
- Confirm notification and background workers are active.

## Weekly
- Review SLA, satisfaction and queue trends.
- Review failed logins, lockouts and suspicious rate-limit activity.
- Check S3 evidence access and database growth.

## Monthly
- Test an RDS snapshot restore in non-production.
- Review dependencies and security advisories.
- Review inactive accounts and governance catalogs.
- Export and archive operational evidence.

## Per release
- Run Phase 14 quality gates.
- Snapshot RDS.
- Record image tags, commit SHA and migration plan.
- Verify rollback.
- Update changelog and handover evidence.

## Ownership
Assign named owners for application, database, storage, security, municipal operations and stakeholder communication before launch.
