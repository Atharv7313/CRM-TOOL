-- Schema for the ReportingService domain tables in LifeSci360_Services.
-- The shared Users/Roles/UserSessions tables are assumed to already exist
-- (created by the Auth baseline). This script creates the 11 clinical-trial
-- tables the KPI calculations read from. Idempotent: safe to re-run.

-- ── Site ───────────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Sites', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sites
    (
        SiteId   uniqueidentifier NOT NULL CONSTRAINT DF_Sites_SiteId DEFAULT (newsequentialid()),
        Name     nvarchar(150)    NOT NULL,
        Location nvarchar(255)    NULL,
        IsActive bit              NOT NULL CONSTRAINT DF_Sites_IsActive DEFAULT (1),
        CONSTRAINT PK_Sites PRIMARY KEY (SiteId)
    );
END;

-- ── Protocol ───────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Protocols', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Protocols
    (
        ProtocolId      uniqueidentifier NOT NULL CONSTRAINT DF_Protocols_ProtocolId DEFAULT (newsequentialid()),
        Title           nvarchar(200)    NOT NULL,
        Phase           nvarchar(50)     NOT NULL,
        Status          nvarchar(50)     NOT NULL CONSTRAINT DF_Protocols_Status DEFAULT ('DRAFT'),
        Description      nvarchar(max)    NULL,
        StartDate        datetime2(0)     NULL,
        EndDate          datetime2(0)     NULL,
        CreatedByUserId  uniqueidentifier NOT NULL,
        CONSTRAINT PK_Protocols PRIMARY KEY (ProtocolId),
        CONSTRAINT FK_Protocols_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserId)
    );
    CREATE INDEX IX_Protocols_CreatedBy ON dbo.Protocols (CreatedByUserId);
    CREATE INDEX IX_Protocols_Status    ON dbo.Protocols (Status);
END;

-- ── Patient ────────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Patients', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Patients
    (
        PatientId     uniqueidentifier NOT NULL CONSTRAINT DF_Patients_PatientId DEFAULT (newsequentialid()),
        Name          nvarchar(100)    NOT NULL,
        DateOfBirth   date             NOT NULL,
        ContactInfo   nvarchar(255)    NULL,
        PatientStatus nvarchar(50)     NOT NULL CONSTRAINT DF_Patients_PatientStatus DEFAULT ('ACTIVE'),
        CreatedAt     datetime2(0)     NOT NULL CONSTRAINT DF_Patients_CreatedAt DEFAULT (getutcdate()),
        CONSTRAINT PK_Patients PRIMARY KEY (PatientId)
    );
    CREATE INDEX IX_Patients_PatientStatus ON dbo.Patients (PatientStatus);
END;

-- ── ProtocolSite ───────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.ProtocolSites', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProtocolSites
    (
        ProtocolSiteId     uniqueidentifier NOT NULL CONSTRAINT DF_ProtocolSites_Id DEFAULT (newsequentialid()),
        ProtocolId         uniqueidentifier NOT NULL,
        SiteId             uniqueidentifier NOT NULL,
        InvestigatorUserId uniqueidentifier NOT NULL,
        Status             nvarchar(50)     NOT NULL CONSTRAINT DF_ProtocolSites_Status DEFAULT ('ACTIVE'),
        CONSTRAINT PK_ProtocolSites PRIMARY KEY (ProtocolSiteId),
        CONSTRAINT FK_ProtocolSites_ProtocolId   FOREIGN KEY (ProtocolId)         REFERENCES dbo.Protocols (ProtocolId),
        CONSTRAINT FK_ProtocolSites_SiteId       FOREIGN KEY (SiteId)             REFERENCES dbo.Sites (SiteId),
        CONSTRAINT FK_ProtocolSites_Investigator FOREIGN KEY (InvestigatorUserId) REFERENCES dbo.Users (UserId)
    );
    CREATE INDEX IX_ProtocolSites_Protocol ON dbo.ProtocolSites (ProtocolId);
    CREATE INDEX IX_ProtocolSites_Site     ON dbo.ProtocolSites (SiteId);
    CREATE UNIQUE INDEX UQ_ProtocolSites   ON dbo.ProtocolSites (ProtocolId, SiteId);
END;

-- ── PatientEnrollment ──────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.PatientEnrollments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientEnrollments
    (
        EnrollmentId     uniqueidentifier NOT NULL CONSTRAINT DF_PatientEnrollments_Id DEFAULT (newsequentialid()),
        PatientId        uniqueidentifier NOT NULL,
        ProtocolSiteId   uniqueidentifier NOT NULL,
        EnrollmentStatus nvarchar(50)     NOT NULL CONSTRAINT DF_PatientEnrollments_Status DEFAULT ('ACTIVE'),
        EnrolledAt       datetime2(0)     NOT NULL CONSTRAINT DF_PatientEnrollments_EnrolledAt DEFAULT (getutcdate()),
        CONSTRAINT PK_PatientEnrollments PRIMARY KEY (EnrollmentId),
        CONSTRAINT FK_PatientEnrollments_PatientId    FOREIGN KEY (PatientId)      REFERENCES dbo.Patients (PatientId),
        CONSTRAINT FK_PatientEnrollments_ProtocolSite FOREIGN KEY (ProtocolSiteId) REFERENCES dbo.ProtocolSites (ProtocolSiteId)
    );
    CREATE INDEX IX_Enrollments_PatientId    ON dbo.PatientEnrollments (PatientId);
    CREATE INDEX IX_Enrollments_ProtocolSite ON dbo.PatientEnrollments (ProtocolSiteId);
    CREATE UNIQUE INDEX UQ_PatientEnrollments ON dbo.PatientEnrollments (PatientId, ProtocolSiteId);
END;

-- ── Sample ─────────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Samples', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Samples
    (
        SampleId          uniqueidentifier NOT NULL CONSTRAINT DF_Samples_SampleId DEFAULT (newsequentialid()),
        EnrollmentId      uniqueidentifier NOT NULL,
        CollectedByUserId uniqueidentifier NOT NULL,
        SampleType        nvarchar(100)    NOT NULL,
        CollectedDate     datetime2(0)     NOT NULL,
        Status            nvarchar(50)     NOT NULL CONSTRAINT DF_Samples_Status DEFAULT ('COLLECTED'),
        CONSTRAINT PK_Samples PRIMARY KEY (SampleId),
        CONSTRAINT FK_Samples_EnrollmentId      FOREIGN KEY (EnrollmentId)      REFERENCES dbo.PatientEnrollments (EnrollmentId),
        CONSTRAINT FK_Samples_CollectedByUserId FOREIGN KEY (CollectedByUserId) REFERENCES dbo.Users (UserId)
    );
    CREATE INDEX IX_Samples_CollectedDate ON dbo.Samples (CollectedDate DESC);
    CREATE INDEX IX_Samples_EnrollmentId  ON dbo.Samples (EnrollmentId);
    CREATE INDEX IX_Samples_Status        ON dbo.Samples (Status);
END;

-- ── LabResult ──────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.LabResults', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LabResults
    (
        ResultId         uniqueidentifier NOT NULL CONSTRAINT DF_LabResults_ResultId DEFAULT (newsequentialid()),
        SampleId         uniqueidentifier NOT NULL,
        RecordedByUserId uniqueidentifier NOT NULL,
        TestType         nvarchar(100)    NOT NULL,
        ResultValue      nvarchar(255)    NOT NULL,
        ResultDate       datetime2(0)     NOT NULL,
        CONSTRAINT PK_LabResults PRIMARY KEY (ResultId),
        CONSTRAINT FK_LabResults_SampleId         FOREIGN KEY (SampleId)         REFERENCES dbo.Samples (SampleId),
        CONSTRAINT FK_LabResults_RecordedByUserId FOREIGN KEY (RecordedByUserId) REFERENCES dbo.Users (UserId)
    );
    CREATE INDEX IX_LabResults_SampleId ON dbo.LabResults (SampleId);
END;

-- ── Visit ──────────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Visits', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Visits
    (
        VisitId      uniqueidentifier NOT NULL CONSTRAINT DF_Visits_VisitId DEFAULT (newsequentialid()),
        EnrollmentId uniqueidentifier NOT NULL,
        VisitName    nvarchar(200)    NOT NULL,
        VisitDate    datetime2(0)     NOT NULL,
        VisitStatus  nvarchar(50)     NOT NULL CONSTRAINT DF_Visits_VisitStatus DEFAULT ('SCHEDULED'),
        CONSTRAINT PK_Visits PRIMARY KEY (VisitId),
        CONSTRAINT FK_Visits_EnrollmentId FOREIGN KEY (EnrollmentId) REFERENCES dbo.PatientEnrollments (EnrollmentId)
    );
    CREATE INDEX IX_Visits_EnrollmentId ON dbo.Visits (EnrollmentId);
    CREATE INDEX IX_Visits_VisitDate    ON dbo.Visits (VisitDate DESC);
    CREATE INDEX IX_Visits_VisitStatus  ON dbo.Visits (VisitStatus);
END;

-- ── ComplianceReport ───────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.ComplianceReports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ComplianceReports
    (
        ReportId          uniqueidentifier NOT NULL CONSTRAINT DF_ComplianceReports_Id DEFAULT (newsequentialid()),
        ProtocolId        uniqueidentifier NOT NULL,
        GeneratedByUserId uniqueidentifier NOT NULL,
        Scope             nvarchar(100)    NOT NULL,
        Metrics           nvarchar(max)    NULL,
        Status            nvarchar(50)     NOT NULL CONSTRAINT DF_ComplianceReports_Status DEFAULT ('DRAFT'),
        GeneratedAt       datetime2(0)     NOT NULL CONSTRAINT DF_ComplianceReports_GeneratedAt DEFAULT (getutcdate()),
        CONSTRAINT PK_ComplianceReports PRIMARY KEY (ReportId),
        CONSTRAINT FK_ComplianceReports_ProtocolId        FOREIGN KEY (ProtocolId)        REFERENCES dbo.Protocols (ProtocolId),
        CONSTRAINT FK_ComplianceReports_GeneratedByUserId FOREIGN KEY (GeneratedByUserId) REFERENCES dbo.Users (UserId)
    );
    CREATE INDEX IX_ComplianceReports_Protocol ON dbo.ComplianceReports (ProtocolId);
END;

-- ── KpiReport ──────────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.KpiReports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KpiReports
    (
        ReportId          uniqueidentifier NOT NULL CONSTRAINT DF_KpiReports_Id DEFAULT (newsequentialid()),
        ProtocolId        uniqueidentifier NULL,
        GeneratedByUserId uniqueidentifier NOT NULL,
        Scope             nvarchar(100)    NOT NULL,
        Metrics           nvarchar(max)    NULL,
        GeneratedAt       datetime2(0)     NOT NULL CONSTRAINT DF_KpiReports_GeneratedAt DEFAULT (getutcdate()),
        CONSTRAINT PK_KpiReports PRIMARY KEY (ReportId),
        CONSTRAINT FK_KpiReports_ProtocolId        FOREIGN KEY (ProtocolId)        REFERENCES dbo.Protocols (ProtocolId),
        CONSTRAINT FK_KpiReports_GeneratedByUserId FOREIGN KEY (GeneratedByUserId) REFERENCES dbo.Users (UserId)
    );
    CREATE INDEX IX_KpiReports_Protocol ON dbo.KpiReports (ProtocolId);
END;

-- ── Notification ───────────────────────────────────────────────────────────
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications
    (
        NotificationId uniqueidentifier NOT NULL CONSTRAINT DF_Notifications_Id DEFAULT (newsequentialid()),
        UserId         uniqueidentifier NOT NULL,
        Category       nvarchar(100)    NOT NULL,
        Message        nvarchar(max)    NOT NULL,
        Channel        nvarchar(50)     NOT NULL CONSTRAINT DF_Notifications_Channel DEFAULT ('IN_APP'),
        Status         nvarchar(50)     NOT NULL CONSTRAINT DF_Notifications_Status DEFAULT ('UNREAD'),
        CreatedAt      datetime2(0)     NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (getutcdate()),
        ReadAt         datetime2(0)     NULL,
        CONSTRAINT PK_Notifications PRIMARY KEY (NotificationId),
        CONSTRAINT FK_Notifications_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
    );
    CREATE INDEX IX_Notifications_CreatedAt ON dbo.Notifications (CreatedAt DESC);
    CREATE INDEX IX_Notifications_Status    ON dbo.Notifications (Status);
    CREATE INDEX IX_Notifications_UserId    ON dbo.Notifications (UserId);
END;
