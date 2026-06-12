namespace NotificationService.Data;

internal static class NotificationsBootstrap
{
    public const string Sql = @"
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.Notifications', N'SentByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Notifications ADD SentByUserId uniqueidentifier NULL;
END;
";
}
