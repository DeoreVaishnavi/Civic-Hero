-- REVIEW BEFORE RUNNING. This performs a safe soft deletion and session revocation.
START TRANSACTION;

DELETE poc
FROM phone_otp_challenges poc
INNER JOIN users u ON u.Id = poc.UserId
WHERE u.IsSystemAccount = 0
  AND LOWER(u.Email) IN ('your-old-superadmin-email@example.com', 'your-test-citizen@example.com', 'your-test-officer@example.com');

UPDATE users
SET IsActive = 0,
    IsDeleted = 1,
    DeletedAt = UTC_TIMESTAMP(),
    RefreshTokenHash = NULL,
    RefreshTokenCreatedAt = NULL,
    RefreshTokenExpiresAt = NULL,
    AuthorizationVersion = AuthorizationVersion + 1,
    FailedLoginAttempts = 0,
    LockoutEnd = NULL,
    TwoFactorEnabled = 0,
    TwoFactorSecretProtected = NULL,
    TwoFactorRecoveryCodesJson = NULL,
    TwoFactorEnabledAt = NULL
WHERE IsSystemAccount = 0
  AND LOWER(Email) IN ('your-old-superadmin-email@example.com', 'your-test-citizen@example.com', 'your-test-officer@example.com');

SELECT Id, Email, Role, IsActive, IsDeleted, IsSystemAccount
FROM users
WHERE LOWER(Email) IN ('your-old-superadmin-email@example.com', 'your-test-citizen@example.com', 'your-test-officer@example.com');

COMMIT;
