-- Baseline tables for ServicesDbContext (Roles, Users, UserSessions).
-- These are treated as a pre-existing baseline by the EF model snapshot,
-- so no migration creates them. Run once against a fresh LifeSci360_Services DB,
-- then apply EF migrations (AddAuditLog) on top.

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RoleId   uniqueidentifier NOT NULL CONSTRAINT DF_Roles_RoleId DEFAULT (newsequentialid()),
        IsActive bit              NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT (1),
        RoleName nvarchar(50)     NOT NULL,
        CONSTRAINT PK_Roles PRIMARY KEY (RoleId)
    );
    CREATE UNIQUE INDEX UQ_Roles_RoleName ON dbo.Roles (RoleName);
END;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId       uniqueidentifier NOT NULL CONSTRAINT DF_Users_UserId DEFAULT (newsequentialid()),
        CreatedAt    datetime2        NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (getutcdate()),
        Email        nvarchar(150)    NOT NULL,
        IsActive     bit              NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        Name         nvarchar(100)    NOT NULL,
        PasswordHash nvarchar(255)    NOT NULL,
        Phone        nvarchar(20)     NULL,
        RoleId       uniqueidentifier NOT NULL,
        CONSTRAINT PK_Users PRIMARY KEY (UserId),
        CONSTRAINT FK_Users_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.Roles (RoleId)
    );
    CREATE INDEX IX_Users_Email ON dbo.Users (Email);
    CREATE INDEX IX_Users_RoleId ON dbo.Users (RoleId);
    CREATE UNIQUE INDEX UQ_Users_Email ON dbo.Users (Email);
END;

IF OBJECT_ID(N'dbo.UserSessions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSessions
    (
        SessionId uniqueidentifier NOT NULL CONSTRAINT DF_UserSessions_SessionId DEFAULT (newsequentialid()),
        CreatedAt datetime2(0)     NOT NULL CONSTRAINT DF_UserSessions_CreatedAt DEFAULT (getutcdate()),
        ExpiresAt datetime2(0)     NOT NULL,
        IpAddress nvarchar(45)     NULL,
        IsRevoked bit              NOT NULL,
        TokenJti  nvarchar(100)    NOT NULL,
        UserAgent nvarchar(500)    NULL,
        UserId    uniqueidentifier NOT NULL,
        CONSTRAINT PK_UserSessions PRIMARY KEY (SessionId),
        CONSTRAINT FK_UserSessions_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId) ON DELETE CASCADE
    );
    CREATE INDEX IX_UserSessions_ExpiresAt ON dbo.UserSessions (ExpiresAt);
    CREATE INDEX IX_UserSessions_TokenJti ON dbo.UserSessions (TokenJti);
    CREATE INDEX IX_UserSessions_UserId ON dbo.UserSessions (UserId);
    CREATE UNIQUE INDEX UQ_UserSessions_TokenJti ON dbo.UserSessions (TokenJti);
END;
