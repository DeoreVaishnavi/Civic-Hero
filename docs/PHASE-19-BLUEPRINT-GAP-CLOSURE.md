# CivicHero Add-on 3 / Phase 19 — Blueprint Gap Closure

## Purpose

This add-on implements five blueprint workflows that were missing or only partially covered after the original 18-phase roadmap:

1. Anonymous complaint reporting and private token-based tracking.
2. Verified phone-number authentication and SMS one-time passwords.
3. Complaint comments with visibility, moderation and notifications.
4. Citizen “possible emergency” flags with controlled official review.
5. Advisory multimodal before/after visual verification with human fallback.

## Security model

### Anonymous complaints

Anonymous requests are protected by CAPTCHA, route-specific rate limits, an additional per-IP hashed submission limit and strict file validation. The application creates a disabled internal system-reporter record so existing non-null complaint ownership and foreign keys remain intact. Operational users only see that the complaint is anonymous.

The tracking token is generated using cryptographically secure random bytes and stored only as a SHA-256 hash. It is shown once to the reporter. Tracking requests submit the token in the HTTPS request body rather than placing it in the URL or browser history. Optional contact information is encrypted with ASP.NET Core Data Protection and is accepted only after explicit consent.

Anonymous complaints:

- do not earn rewards;
- cannot use account-based citizen verification;
- cannot raise account-based disputes or appeals;
- expose only a limited public timeline through the private tracking token;
- do not expose the internal system-reporter account.

### Phone OTP

Phone numbers are normalized to an E.164-style format and uniquely indexed. A phone number must be verified before password or OTP login by phone is allowed. OTPs are six digits, hashed in the database, expire after a configurable period, enforce resend cooldowns, maximum requests per hour and maximum failed attempts.

The Development provider writes OTP messages to the backend log and returns the code only in Development. The Twilio provider calls the Messages REST API using credentials stored outside source control.

### Comments

Comments support `Public` and `Internal` visibility. Citizens can add only public comments. Officers, Supervisors and Administrators can add internal operational notes. Public comments pass an automatic high-risk-pattern check and can be hidden for moderator review. Supervisors moderate within their department; Admin and SuperAdmin can moderate citywide.

### Emergency review

Citizens and anonymous reporters can mark a complaint as a **possible emergency** and must provide a reason. The system temporarily uses High attention, but it does not allow a citizen to self-assign Critical priority. A Supervisor, Admin or SuperAdmin must confirm High/Critical priority or reject the emergency flag with an official reason.

### AI visual verification

The visual pipeline compares original and resolution evidence only after both groups exist. The default rule-based provider validates evidence presence and metadata but never auto-confirms physical completion. The optional Gemini provider submits protected before/after images as multimodal input and requests structured JSON containing:

- verdict;
- completion score;
- image-quality score;
- manipulation-risk score;
- confidence;
- reasoning and observations.

AI output is advisory. Low confidence, suspicious evidence, manipulation risk or non-resolved verdicts always require human review. Human reviewers can confirm evidence, request rework or dismiss the AI flag.

## New database tables

- `phone_otp_challenges`
- `anonymous_complaint_access`
- `complaint_comments`
- `complaint_emergency_reviews`
- `visual_verification_analyses`

## Extended tables

- `users`: normalized phone, phone verification and system-account flags.
- `complaints`: anonymous-reporting and emergency-review metadata.

## New public routes

- `/report-anonymously`
- `/track-anonymous`

## New protected routes

- `/supervisor/emergency-reviews`
- `/supervisor/visual-verification`
- `/admin/emergency-reviews`
- `/admin/visual-verification`

## Main API endpoints

```text
POST /api/v1/anonymous-complaints
POST /api/v1/anonymous-complaints/track

POST /api/v1/auth/phone/request-login-otp
POST /api/v1/auth/phone/login
POST /api/v1/auth/phone/request-verification
POST /api/v1/auth/phone/verify

GET    /api/v1/complaints/{id}/comments
POST   /api/v1/complaints/{id}/comments
DELETE /api/v1/complaints/{id}/comments/{commentId}
POST   /api/v1/complaints/{id}/comments/{commentId}/moderate

GET  /api/v1/emergency-reviews
POST /api/v1/emergency-reviews/{id}/decision

GET  /api/v1/visual-verification
GET  /api/v1/visual-verification/{id}
POST /api/v1/visual-verification/complaints/{complaintId}/analyze
POST /api/v1/visual-verification/{id}/review
```

## Migration

`Phase19BlueprintGapClosure`

Review the generated migration before applying it to AWS RDS. Only one teammate should generate/apply the migration.

## Production configuration

Local Windows Development can use `.NET user-secrets`. Deployed containers or AWS environments must use environment variables or the Phase 15 secret-mount mechanism. Never commit CAPTCHA secrets, Twilio credentials or Gemini keys.
