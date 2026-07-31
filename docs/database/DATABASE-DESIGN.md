# CivicHero Database Design

## Platform
AWS RDS for MySQL accessed through EF Core.

## Principal table groups
- Identity: `users`, refresh/session fields, roles and scope.
- Geography and governance: `departments`, `wards`, `complaint_categories`, `system_settings`.
- Complaints: `complaints`, `complaint_images`, `complaint_timeline`.
- Work management: `complaint_assignments`, `complaint_progress_updates`.
- Verification and disputes: `complaint_verifications`, disputes and dispute audit logs.
- Engagement: notifications, preferences, votes, rewards, badges, reputation and redemptions.
- AI: triage and fraud analyses.
- Assistance: chat sessions and chat messages.
- Governance: immutable audit logs.

## Integrity rules
- Foreign keys preserve user, complaint, department and ward relationships.
- Unique indexes prevent duplicate reward ledger entries.
- Refresh tokens are stored as hashes.
- Deactivation is preferred over destructive deletion for governance catalogs.
- Audit records are append-only through normal application workflows.

## Operational rules
- Apply migrations once through an approved deployment owner.
- Snapshot RDS before production schema changes.
- Never use `EnsureCreated` in production.
- Validate restore procedures in a non-production environment.
- Retain complaint evidence and audit records according to municipal policy.
