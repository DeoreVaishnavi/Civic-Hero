
# Phase 17 — Final Integration and Gap Closure

Phase 17 connects the infrastructure placeholders that remained after the feature phases.

## Delivered

- Redis distributed cache with memory fallback.
- Redis readiness health check.
- RabbitMQ publisher, durable consumer, retry queue and dead-letter queue.
- RabbitMQ readiness health check.
- SLA breach monitoring, priority escalation and verification auto-close workers.
- Authenticator-app two-factor authentication with one-time recovery codes.
- Persistent ASP.NET Data Protection keys for encrypted 2FA secrets.
- Global input sanitization for DTO string properties.
- Admin Integration Centre and runtime test endpoints.
- Local Docker Redis and RabbitMQ services.
- Automated empty-file and mock-data gap audit.

## Safety rules

Redis and RabbitMQ are non-blocking dependencies for normal complaint operations. Cache failures fall back to direct database reads. RabbitMQ failures are logged and retried without deleting complaint data. Background workers use idempotent status filters so already-closed complaints are not processed again.

## Migration

The `Phase17IntegrationHardening` migration adds the following user columns:

- `TwoFactorEnabled`
- `TwoFactorSecretProtected`
- `TwoFactorRecoveryCodesJson`
- `TwoFactorEnabledAt`

Never store raw authenticator secrets or recovery codes in logs, source control or screenshots.
