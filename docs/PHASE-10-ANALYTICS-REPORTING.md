# CivicHero Phase 10 — Analytics & Reporting

Phase 10 turns CivicHero's operational data into role-scoped decision support.

## Backend

- `GET /api/v1/analytics/overview`
- `GET /api/v1/analytics/complaints`
- `GET /api/v1/analytics/departments`
- `GET /api/v1/analytics/officers`
- `GET /api/v1/analytics/wards`
- `GET /api/v1/analytics/sla`
- `GET /api/v1/analytics/satisfaction`
- `GET /api/v1/analytics/heatmap`
- `GET /api/v1/analytics/export?report=complaints|departments|officers|wards|sla`

Supervisor queries are automatically restricted to the department in their JWT. Admin and SuperAdmin can view citywide data. CSV export is Admin/SuperAdmin only.

## Frontend

- Admin analytics command centre
- Date-range filtering
- Complaint trend and status charts
- Department ranking
- Ward heatmap-ready visualization
- Admin CSV export centre
- Supervisor team analytics and officer scorecard

## Database

No new table or migration is required. Metrics are calculated from existing Phase 2–9 tables.
