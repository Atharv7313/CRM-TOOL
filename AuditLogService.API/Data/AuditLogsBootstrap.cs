namespace AuditLogService.API.Data;

internal static class AuditLogsBootstrap
{
    public const string Sql = @"
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        Id            int              IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        ActorUserId   uniqueidentifier NULL,
        ActorName     nvarchar(100)    NOT NULL CONSTRAINT DF_AuditLogs_ActorName    DEFAULT (''),
        ActorEmail    nvarchar(150)    NULL,
        [Action]      nvarchar(100)    NOT NULL CONSTRAINT DF_AuditLogs_Action       DEFAULT (''),
        ServiceName   nvarchar(50)     NOT NULL CONSTRAINT DF_AuditLogs_ServiceName  DEFAULT (''),
        [Description] nvarchar(500)    NULL,
        EntityId      nvarchar(100)    NULL,
        EntityName    nvarchar(200)    NULL,
        IpAddress     nvarchar(45)     NULL,
        IsSuccess     bit              NOT NULL CONSTRAINT DF_AuditLogs_IsSuccess    DEFAULT (1),
        ErrorMessage  nvarchar(max)    NULL,
        CreatedAt     datetime2        NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt    DEFAULT (getutcdate())
    );
END
ELSE
BEGIN
    IF COL_LENGTH(N'dbo.AuditLogs', N'Id') IS NULL
    BEGIN
        ALTER TABLE dbo.AuditLogs ADD Id int IDENTITY(1,1) NOT NULL;
        IF NOT EXISTS (SELECT 1 FROM sys.key_constraints
                       WHERE parent_object_id = OBJECT_ID(N'dbo.AuditLogs') AND [type] = 'PK')
            ALTER TABLE dbo.AuditLogs ADD CONSTRAINT PK_AuditLogs PRIMARY KEY (Id);
    END
    IF COL_LENGTH(N'dbo.AuditLogs', N'ActorUserId')  IS NULL ALTER TABLE dbo.AuditLogs ADD ActorUserId   uniqueidentifier NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'ActorName')    IS NULL ALTER TABLE dbo.AuditLogs ADD ActorName     nvarchar(100) NOT NULL CONSTRAINT DF_AuditLogs_ActorName   DEFAULT ('');
    IF COL_LENGTH(N'dbo.AuditLogs', N'ActorEmail')   IS NULL ALTER TABLE dbo.AuditLogs ADD ActorEmail    nvarchar(150) NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'Action')       IS NULL ALTER TABLE dbo.AuditLogs ADD [Action]      nvarchar(100) NOT NULL CONSTRAINT DF_AuditLogs_Action      DEFAULT ('');
    IF COL_LENGTH(N'dbo.AuditLogs', N'ServiceName')  IS NULL ALTER TABLE dbo.AuditLogs ADD ServiceName   nvarchar(50)  NOT NULL CONSTRAINT DF_AuditLogs_ServiceName DEFAULT ('');
    IF COL_LENGTH(N'dbo.AuditLogs', N'Description')  IS NULL ALTER TABLE dbo.AuditLogs ADD [Description] nvarchar(500) NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'EntityId')     IS NULL ALTER TABLE dbo.AuditLogs ADD EntityId      nvarchar(100) NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'EntityName')   IS NULL ALTER TABLE dbo.AuditLogs ADD EntityName    nvarchar(200) NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'IpAddress')    IS NULL ALTER TABLE dbo.AuditLogs ADD IpAddress     nvarchar(45)  NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'IsSuccess')    IS NULL ALTER TABLE dbo.AuditLogs ADD IsSuccess     bit           NOT NULL CONSTRAINT DF_AuditLogs_IsSuccess   DEFAULT (1);
    IF COL_LENGTH(N'dbo.AuditLogs', N'ErrorMessage') IS NULL ALTER TABLE dbo.AuditLogs ADD ErrorMessage  nvarchar(max) NULL;
    IF COL_LENGTH(N'dbo.AuditLogs', N'CreatedAt')    IS NULL ALTER TABLE dbo.AuditLogs ADD CreatedAt     datetime2     NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt   DEFAULT (getutcdate());
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_ServiceName' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_ServiceName ON dbo.AuditLogs (ServiceName);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_ActorUserId'  AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_ActorUserId  ON dbo.AuditLogs (ActorUserId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_CreatedAt'    AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_CreatedAt    ON dbo.AuditLogs (CreatedAt);
";
}
