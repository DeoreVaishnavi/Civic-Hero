# CivicHero Phase 12 — Administration, Audit & System Governance

## Scope

Phase 12 completes the core Administration module:

- Complaint-category catalog
- Department and ward master-data management
- Immutable audit logging for mutating API actions
- Searchable and exportable audit history
- Runtime system settings
- Dependency health and process telemetry
- Safe maintenance preview and cleanup

## Security

- All `/api/v1/admin/*` endpoints require Admin or SuperAdmin.
- System-setting changes and destructive cleanup require SuperAdmin.
- Audit logs never store passwords, tokens, uploaded files, or raw request bodies.
- Every POST, PUT, PATCH and DELETE controller action is recorded with its correlation ID.

## Database

Migration: `Phase12AdministrationAudit`

New tables:

- `complaint_categories`
- `system_settings`
- `audit_logs`

## Admin screens

- `/admin/governance`
- `/admin/audit-logs`
- `/admin/system-health`
