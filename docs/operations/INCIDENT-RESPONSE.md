# CivicHero Incident Response Runbook

## Severity
- **Critical:** security breach, total outage, data corruption or irreversible evidence loss.
- **Major:** core complaint workflow unavailable or broad authentication failure.
- **Minor:** limited feature degradation with a workaround.

## Response
1. Record start time, reporter and correlation IDs.
2. Protect users and evidence; disable risky operations when needed.
3. Identify affected release, infrastructure and user scope.
4. Preserve logs and audit evidence.
5. Apply containment or rollback.
6. Verify health and critical workflows.
7. Communicate status without exposing sensitive details.
8. Complete root-cause and prevention actions.

## Never
- Delete logs to hide a failure.
- publish credentials in chat or tickets.
- run unreviewed database commands in production.
- declare recovery before critical workflows and data integrity are verified.
