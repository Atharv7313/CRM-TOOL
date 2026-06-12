-- Seeds the project's actor roles and one demo user per role so the platform
-- has realistic recipients (notifications, user management, etc.).
-- All seeded users share the password Admin@1234 (same BCrypt hash as the admin).
-- Idempotent: roles/users are only inserted if missing.

SET NOCOUNT ON;

DECLARE @pwd nvarchar(255) = N'$2a$11$g4bt07Fo/giZmW2vMYOL5eGlwR1K/DhKuxZr7lRQrKdM17hmuaB76';

-- ── Roles ───────────────────────────────────────────────────────────────────
DECLARE @roles TABLE (Name nvarchar(50));
INSERT INTO @roles (Name) VALUES
    (N'ADMIN'),
    (N'RESEARCH_SCIENTIST'),
    (N'LAB_TECHNICIAN'),
    (N'CLINICAL_TRIAL_MANAGER'),
    (N'REGULATORY_OFFICER'),
    (N'DATA_MANAGER');

INSERT INTO dbo.Roles (RoleName, IsActive)
SELECT r.Name, 1
FROM @roles r
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles x WHERE x.RoleName = r.Name);

-- ── Users (one per actor role) ──────────────────────────────────────────────
DECLARE @users TABLE (Name nvarchar(100), Email nvarchar(150), RoleName nvarchar(50));
INSERT INTO @users (Name, Email, RoleName) VALUES
    (N'Alex Morgan',     N'alex.morgan@lifesci360.com',   N'ADMIN'),
    (N'Dr. Sarah Chen',  N'sarah.chen@lifesci360.com',    N'RESEARCH_SCIENTIST'),
    (N'Marco Rossi',     N'marco.rossi@lifesci360.com',   N'LAB_TECHNICIAN'),
    (N'Priya Nair',      N'priya.nair@lifesci360.com',    N'CLINICAL_TRIAL_MANAGER'),
    (N'James Okafor',    N'james.okafor@lifesci360.com',  N'REGULATORY_OFFICER'),
    (N'Lena Schmidt',    N'lena.schmidt@lifesci360.com',  N'DATA_MANAGER');

INSERT INTO dbo.Users (Email, Name, PasswordHash, Phone, RoleId, IsActive, CreatedAt)
SELECT u.Email, u.Name, @pwd, NULL, r.RoleId, 1, getutcdate()
FROM @users u
JOIN dbo.Roles r ON r.RoleName = u.RoleName
WHERE NOT EXISTS (SELECT 1 FROM dbo.Users x WHERE x.Email = u.Email);

PRINT 'Actor roles and demo users seeded.';
