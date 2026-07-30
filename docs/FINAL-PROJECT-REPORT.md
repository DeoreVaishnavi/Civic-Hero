# CivicHero Final Project Report

## Abstract
CivicHero is a full-stack civic complaint governance platform designed to improve transparency between citizens and municipal teams. The system supports geo-tagged reporting, evidence management, assignment, SLA tracking, citizen verification, disputes, real-time notifications, rewards, AI-assisted triage, analytics, chatbot assistance, governance and auditability.

## Technology
- React and Vite frontend.
- ASP.NET Core 8 Web API.
- EF Core and AWS RDS MySQL.
- Amazon S3 private evidence storage.
- SignalR notifications.
- Docker, Nginx and GitHub Actions deployment assets.

## Engineering outcomes
The project applies role-based security, refresh-token rotation, scoped permissions, workflow timelines, idempotent rewards, advisory AI with deterministic fallback, health checks, rate limits, automated testing assets, container deployment and operational documentation.

## Evaluation demonstration
Demonstrate a complete journey: Citizen submits complaint → Supervisor assigns → Officer resolves → Citizen verifies → notifications/rewards update → Admin reviews analytics and audit history.

## Honest limitations
External email/SMS delivery, production infrastructure approval, real stakeholder UAT and formal municipal policies must be completed by the deploying organisation. Prepared artifacts do not replace real execution evidence.
