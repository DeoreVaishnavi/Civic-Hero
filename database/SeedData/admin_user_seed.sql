USE civicherodb;
INSERT INTO Users
(FullName, Email, PhoneNumber, PasswordHash, Role, ReputationPoints, IsActive, CreatedAt, UpdatedAt)
VALUES
('Super Admin', 'admin@civichero.local', '9999999999', 'TEMP_PASSWORD_HASH_CHANGE_IN_PART_2', 'Super', 0, 1, UTC_TIMESTAMP(), NULL);