using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        LogId          int              IDENTITY(1,1) NOT NULL,
        ActorUserId    uniqueidentifier NULL,
        ActorName      nvarchar(100)    NOT NULL,
        ActorEmail     nvarchar(150)    NOT NULL,
        Action         nvarchar(50)     NOT NULL,
        Description    nvarchar(500)    NOT NULL,
        TargetUserId   uniqueidentifier NULL,
        TargetUserName nvarchar(100)    NULL,
        IpAddress      nvarchar(45)     NULL,
        IsSuccess      bit              NOT NULL CONSTRAINT DF_AuditLogs_IsSuccess DEFAULT (1),
        CreatedAt      datetime2        NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT (getutcdate()),
        CONSTRAINT PK_AuditLogs PRIMARY KEY (LogId),
        CONSTRAINT FK_AuditLogs_Users_ActorUserId FOREIGN KEY (ActorUserId)
            REFERENCES dbo.Users (UserId) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_ActorUserId' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
BEGIN
    CREATE INDEX IX_AuditLogs_ActorUserId ON dbo.AuditLogs (ActorUserId);
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.AuditLogs;
END;
");
        }
    }
}
