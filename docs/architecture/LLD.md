# CivicHero Low-Level Design

## Backend layers
- `Controllers`: HTTP endpoints and role policies.
- `Core/DTOs`: request and response contracts.
- `Core/Entities`: persistence models.
- `Core/Interfaces`: service and repository abstractions.
- `Core/Services`: workflow rules.
- `Infrastructure/Data`: EF Core context and mappings.
- `Infrastructure/Services`: AWS, AI, notifications and background workers.
- `Middleware`: exception handling, logging, correlation, security and rate limits.

## Core workflow state changes
```text
Submitted -> Assigned -> Accepted -> InProgress -> VerificationPending
VerificationPending -> ClosedCitizenApproved
VerificationPending -> Disputed -> Rework / Closed / Appealed
VerificationPending -> ClosedAuto (after configured deadline)
```

## Transaction boundaries
- Assignment/reassignment and history are committed together.
- Resolution submission and evidence metadata are committed together after uploads succeed.
- Reward ledger and redemption balance updates are transactional and idempotent.
- Manual AI/fraud decisions update complaint status and timeline together.

## Frontend design
- `AuthContext` restores refresh-cookie sessions.
- `ProtectedRoute` requires authentication.
- `RoleGuard` prevents cross-role route access.
- Role layouts provide scoped navigation.
- API modules use one configured Axios instance with credentials.
- Pages show explicit loading, error and empty states.

## Error model
Expected failures return safe HTTP status codes and client messages. Unexpected failures receive a correlation ID and are logged without returning secrets or stack traces.
