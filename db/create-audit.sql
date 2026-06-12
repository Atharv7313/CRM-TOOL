-- ============================================================
--  LifeSci360 — Audit Database Setup
--  DATABASE 2: LifeSci360_Audit  (immutable audit log)
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'LifeSci360_Audit')
    CREATE DATABASE LifeSci360_Audit;
GO

USE LifeSci360_Audit;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'lifesci_app')
BEGIN
    CREATE USER lifesci_app FOR LOGIN lifesci_app;
    ALTER ROLE db_owner ADD MEMBER lifesci_app;
END
GO

-- ============================================================
--  AuditLogs — append-only event store
--  Schema matches AuditLogService.API/Models/AuditLog.cs
-- ============================================================

CREATE TABLE AuditLogs (
    Id           INT               NOT NULL  IDENTITY(1,1)  PRIMARY KEY,
    ActorUserId  UNIQUEIDENTIFIER  NULL,
    ActorName    NVARCHAR(100)     NOT NULL,
    ActorEmail   NVARCHAR(150)     NULL,
    Action       NVARCHAR(100)     NOT NULL,
    ServiceName  NVARCHAR(50)      NOT NULL,
    Description  NVARCHAR(500)     NULL,
    EntityId     NVARCHAR(100)     NULL,
    EntityName   NVARCHAR(200)     NULL,
    IpAddress    NVARCHAR(45)      NULL,
    IsSuccess    BIT               NOT NULL,
    ErrorMessage NVARCHAR(MAX)     NULL,
    CreatedAt    DATETIME2         NOT NULL  DEFAULT GETUTCDATE()
);
GO

CREATE NONCLUSTERED INDEX IX_AuditLogs_ServiceName ON AuditLogs(ServiceName);
CREATE NONCLUSTERED INDEX IX_AuditLogs_ActorUserId ON AuditLogs(ActorUserId);
CREATE NONCLUSTERED INDEX IX_AuditLogs_CreatedAt   ON AuditLogs(CreatedAt DESC);
GO

PRINT 'LifeSci360_Audit schema created successfully.';
GO
