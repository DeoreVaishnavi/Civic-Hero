# CivicHero Phase 11 — Citizen Chatbot

## Objective

Provide an authenticated self-service assistant that helps CivicHero users understand the complaint process and retrieve information without bypassing role-based access controls.

## Implemented capabilities

- Start, load and end chat sessions
- Save messages in `chat_sessions` and `chat_messages`
- Rule-based intent detection with no external API dependency
- Complaint-reference lookup using `CH-YYYY-000000`
- Role-scoped complaint visibility
- Recent complaint summaries
- Guided reporting, verification, dispute, reward and notification help
- Quick actions linking users to the correct React screen
- Responsive Citizen Portal assistant page
- Session restoration through browser local storage
- Message-length validation and ownership validation

## Security

- Every endpoint requires JWT authentication.
- A user can only load or end their own chat session.
- Complaint lookups use the same role scope as the main application.
- The assistant never exposes another citizen's complaint.
- Chat answers are advisory and do not perform complaint state transitions.

## API

- `POST /api/v1/chatbot/session`
- `GET /api/v1/chatbot/session/{sessionId}`
- `POST /api/v1/chatbot/message`
- `DELETE /api/v1/chatbot/session/{sessionId}`

## Database

No new migration is required. The existing Phase 2 schema already contains:

- `chat_sessions`
- `chat_messages`
