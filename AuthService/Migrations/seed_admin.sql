-- Seed the system admin account (admin@lifesci360.com / Admin@1234).
-- Idempotent: only inserts if the account is missing.
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Email = N'admin@lifesci360.com')
BEGIN
    INSERT INTO dbo.Users (Email, Name, PasswordHash, Phone, RoleId, IsActive, CreatedAt)
    SELECT
        N'admin@lifesci360.com',
        N'System Administrator',
        N'$2a$11$g4bt07Fo/giZmW2vMYOL5eGlwR1K/DhKuxZr7lRQrKdM17hmuaB76',
        NULL,
        r.RoleId,
        1,
        getutcdate()
    FROM dbo.Roles r
    WHERE r.RoleName = N'SYSTEM_ADMIN';
END;
