-- ============================================
-- Create LifeSci360_Audit Database + Table
-- Run this in SSMS connected to (localdb)\MSSQLLocalDB
-- ============================================

USE master;
GO

-- Drop if exists with wrong schema
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'LifeSci360_Audit')
BEGIN
    ALTER DATABASE LifeSci360_Audit SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE LifeSci360_Audit;
    PRINT 'Dropped existing LifeSci360_Audit database';
END
GO

-- Create fresh database
CREATE DATABASE LifeSci360_Audit;
GO

USE LifeSci360_Audit;
GO

-- Create AuditLogs table with CORRECT schema matching AuditLog.cs model
CREATE TABLE [dbo].[AuditLogs] (
    [Id]           INT            IDENTITY(1,1)   NOT NULL,
    [ActorUserId]  UNIQUEIDENTIFIER               NULL,
    [ActorName]    NVARCHAR(100)  NOT NULL,
    [ActorEmail]   NVARCHAR(150)  NULL,
    [Action]       NVARCHAR(100)  NOT NULL,
    [ServiceName]  NVARCHAR(50)   NOT NULL,
    [Description]  NVARCHAR(500)  NULL,
    [EntityId]     NVARCHAR(100)  NULL,
    [EntityName]   NVARCHAR(200)  NULL,
    [IpAddress]    NVARCHAR(45)   NULL,
    [IsSuccess]    BIT            NOT NULL        DEFAULT(1),
    [ErrorMessage] NVARCHAR(MAX)  NULL,
    [CreatedAt]    DATETIME2      NOT NULL        DEFAULT(GETUTCDATE()),
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

-- Create indexes for fast querying
CREATE INDEX [IX_AuditLogs_ServiceName] ON [dbo].[AuditLogs] ([ServiceName]);
CREATE INDEX [IX_AuditLogs_ActorUserId]  ON [dbo].[AuditLogs] ([ActorUserId]);
CREATE INDEX [IX_AuditLogs_CreatedAt]    ON [dbo].[AuditLogs] ([CreatedAt] DESC);
GO

PRINT 'LifeSci360_Audit database and AuditLogs table created successfully!';
GO
