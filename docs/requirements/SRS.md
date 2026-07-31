# CivicHero Software Requirements Specification

## 1. Purpose
CivicHero is a role-based civic complaint governance platform that helps citizens report public issues and helps municipal teams assign, resolve, verify, audit and analyse those issues.

## 2. Scope
The system contains authentication, role management, complaint reporting, geolocation, S3 evidence, assignment, officer progress, citizen verification, disputes, notifications, rewards, AI-assisted triage, analytics, chatbot support, governance, audit, security, QA and deployment operations.

## 3. Actors
- **Citizen:** registers, reports issues, tracks complaints, verifies resolution, raises disputes and earns points.
- **Officer:** accepts assigned work, records progress and submits resolution evidence.
- **Supervisor:** assigns complaints, monitors SLA, manages workload and reviews disputes.
- **Admin:** manages users, governance, analytics, security, quality and releases.
- **SuperAdmin:** performs protected system-level operations and final administrative decisions.

## 4. Functional requirements
1. Users shall authenticate with verified email, JWT access tokens and rotating refresh tokens.
2. The system shall enforce role, department and ward scope.
3. Citizens shall submit geo-tagged complaints with up to five supported images.
4. Supervisors shall assign and reassign complaints to eligible officers.
5. Officers shall record progress and resolution evidence.
6. Citizens shall approve or reject resolutions within the configured verification window.
7. The system shall preserve complaint, assignment, dispute and audit timelines.
8. Notifications shall be stored and delivered in real time when SignalR is available.
9. Rewards shall be issued idempotently for valid civic contributions.
10. AI outputs shall remain advisory and fall back to deterministic rules.
11. Administrators shall access analytics, exports, governance, audit, security and release readiness.
12. All sensitive write operations shall be authorised and auditable.

## 5. Non-functional requirements
- ASP.NET Core 8 backend and React frontend.
- AWS RDS MySQL database and Amazon S3 private object storage.
- Secure secrets management; no credentials committed to Git.
- Responsive desktop and mobile user interface.
- Graceful degradation when AI, SignalR or external providers are unavailable.
- Health, rate limiting, security headers, correlation IDs and structured logging.
- Automated unit, API, UI, E2E, security and performance test support.
- Containerised deployment behind Nginx with rollback and monitoring procedures.

## 6. Acceptance criteria
Production approval requires real UAT evidence, zero open Critical or Major defects, reviewed security findings, successful backup restoration, approved release evidence and stakeholder sign-off.
