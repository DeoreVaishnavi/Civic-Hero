# Phase 8 — Notifications & Rewards

## Live notifications

- Authenticated SignalR hub: `/hubs/notifications`
- Per-user groups: `user_{userId}`
- Per-role groups: `role_{role}`
- Persistent in-app notification inbox
- Read, read-all, archive and preference APIs
- Admin/SuperAdmin broadcast endpoint

## Rewards

Points are created only after a complaint reaches `Closed` or `ClosedAuto`.

- Valid complaint reaching closure: 10 points
- Approved citizen verification bonus: 20 points
- Community upvotes on the closed complaint: 2 points each
- Auto-closure receives the 10-point valid-complaint award without the verification bonus
- Fraud closures and withdrawn complaints: zero points

The background reward worker is idempotent through a unique reputation-log key.

## Citizen UI

- Live notification bell and unread count
- Notification centre and preferences
- Points balance, lifetime totals, rank and tier
- Achievement badges
- Public leaderboard
- Reward catalog and redemption codes
- Downloadable HTML civic contribution certificate

Email and SMS delivery adapters remain reserved for a later infrastructure phase; Phase 8 stores those preferences but delivers through the persistent in-app inbox and SignalR.
