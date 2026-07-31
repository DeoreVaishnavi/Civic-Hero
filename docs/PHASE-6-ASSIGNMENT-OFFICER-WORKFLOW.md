# CivicHero Phase 6 — Assignment & Officer Workflow

Phase 6 implements the operational handoff from a citizen complaint to municipal field work.

## Backend

- Complaint assignment and reassignment history
- Officer accept/reject workflow
- Department and ward scoped eligible-officer suggestions
- Workload balancing data
- Priority-based assignment and resolution SLAs
- Officer progress updates with optional GPS
- Mandatory S3 resolution evidence
- Resolution transition to `VerificationPending`
- Supervisor and officer dashboard metrics
- Overdue assignment queue

## Frontend

- Officer command dashboard
- Officer work queue and assignment details
- Accept/reject actions
- Progress update form
- Resolution evidence upload
- Supervisor assignment dashboard
- Unassigned/reassignment queue
- Officer workload panel and assignment modal
- Overdue queue

## Migration

The installer creates `Phase6AssignmentOfficerWorkflow`, adding:

- `complaint_assignments`
- `complaint_progress_updates`

All existing complaint and authentication data is preserved.
