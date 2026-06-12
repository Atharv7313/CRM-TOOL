-- ============================================================
--  LifeSci360 — Complete SQL Server Setup
--  DATABASE 1: LifeSci360_Services  (13 application tables)
-- ============================================================


-- ============================================================
--  PRE-REQUISITE: Create login in master FIRST
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'lifesci_app')
BEGIN
    CREATE LOGIN lifesci_app WITH PASSWORD = 'LifeSci@App2024!';
END
GO


-- ============================================================
--  DATABASE 1: LifeSci360_Services
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = 'LifeSci360_Services')
    CREATE DATABASE LifeSci360_Services;
GO

USE LifeSci360_Services;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'lifesci_app')
BEGIN
    CREATE USER lifesci_app FOR LOGIN lifesci_app;
    ALTER ROLE db_owner ADD MEMBER lifesci_app;
END
GO

-- ============================================================
--  MODULE 1 — Identity & Access Management
-- ============================================================

CREATE TABLE Roles (
    RoleId      UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    RoleName    NVARCHAR(50)      NOT NULL,
    IsActive    BIT               NOT NULL  DEFAULT 1,

    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);
GO

CREATE TABLE Users (
    UserId        UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    Name          NVARCHAR(100)     NOT NULL,
    Email         NVARCHAR(150)     NOT NULL,
    Phone         NVARCHAR(20)      NULL,
    PasswordHash  NVARCHAR(255)     NOT NULL,
    RoleId        UNIQUEIDENTIFIER  NOT NULL,
    IsActive      BIT               NOT NULL  DEFAULT 1,
    CreatedAt     DATETIME2(0)      NOT NULL  DEFAULT GETUTCDATE(),

    CONSTRAINT UQ_Users_Email  UNIQUE (Email),
    CONSTRAINT FK_Users_RoleId FOREIGN KEY (RoleId)
        REFERENCES Roles(RoleId)
);
GO

CREATE TABLE UserSessions (
    SessionId  UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    UserId     UNIQUEIDENTIFIER  NOT NULL,
    TokenJti   NVARCHAR(100)     NOT NULL,
    IpAddress  NVARCHAR(45)      NULL,
    UserAgent  NVARCHAR(500)     NULL,
    IsRevoked  BIT               NOT NULL  DEFAULT 0,
    ExpiresAt  DATETIME2(0)      NOT NULL,
    CreatedAt  DATETIME2(0)      NOT NULL  DEFAULT GETUTCDATE(),

    CONSTRAINT UQ_UserSessions_TokenJti UNIQUE (TokenJti),
    CONSTRAINT FK_UserSessions_UserId   FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

-- ============================================================
--  MODULE 2 — Protocol & Study Management
-- ============================================================

CREATE TABLE Protocols (
    ProtocolId       UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    Title            NVARCHAR(200)     NOT NULL,
    Phase            NVARCHAR(50)      NOT NULL,
    Status           NVARCHAR(50)      NOT NULL  DEFAULT 'DRAFT',
    Description      NVARCHAR(MAX)     NULL,
    StartDate        DATETIME2(0)      NULL,
    EndDate          DATETIME2(0)      NULL,
    CreatedByUserId  UNIQUEIDENTIFIER  NOT NULL,

    CONSTRAINT FK_Protocols_CreatedByUserId FOREIGN KEY (CreatedByUserId)
        REFERENCES Users(UserId)
);
GO

CREATE TABLE Sites (
    SiteId    UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    Name      NVARCHAR(150)     NOT NULL,
    Location  NVARCHAR(255)     NULL
);
GO

CREATE TABLE ProtocolSites (
    ProtocolSiteId     UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    ProtocolId         UNIQUEIDENTIFIER  NOT NULL,
    SiteId             UNIQUEIDENTIFIER  NOT NULL,
    InvestigatorUserId UNIQUEIDENTIFIER  NOT NULL,
    Status             NVARCHAR(50)      NOT NULL  DEFAULT 'ACTIVE',

    CONSTRAINT UQ_ProtocolSites              UNIQUE (ProtocolId, SiteId),
    CONSTRAINT FK_ProtocolSites_ProtocolId   FOREIGN KEY (ProtocolId)
        REFERENCES Protocols(ProtocolId) ON DELETE CASCADE,
    CONSTRAINT FK_ProtocolSites_SiteId       FOREIGN KEY (SiteId)
        REFERENCES Sites(SiteId),
    CONSTRAINT FK_ProtocolSites_Investigator FOREIGN KEY (InvestigatorUserId)
        REFERENCES Users(UserId)
);
GO

-- ============================================================
--  MODULE 4 — Patient & Subject Management
-- ============================================================

CREATE TABLE Patients (
    PatientId     UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    Name          NVARCHAR(100)     NOT NULL,
    DateOfBirth   DATE              NOT NULL,
    ContactInfo   NVARCHAR(255)     NULL,
    PatientStatus NVARCHAR(50)      NOT NULL  DEFAULT 'ACTIVE',
    CreatedAt     DATETIME2(0)      NOT NULL  DEFAULT GETUTCDATE()
);
GO

CREATE TABLE PatientEnrollments (
    EnrollmentId      UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    PatientId         UNIQUEIDENTIFIER  NOT NULL,
    ProtocolSiteId    UNIQUEIDENTIFIER  NOT NULL,
    EnrollmentStatus  NVARCHAR(50)      NOT NULL  DEFAULT 'ACTIVE',
    EnrolledAt        DATETIME2(0)      NOT NULL  DEFAULT GETUTCDATE(),

    CONSTRAINT UQ_PatientEnrollments              UNIQUE (PatientId, ProtocolSiteId),
    CONSTRAINT FK_PatientEnrollments_PatientId    FOREIGN KEY (PatientId)
        REFERENCES Patients(PatientId),
    CONSTRAINT FK_PatientEnrollments_ProtocolSite FOREIGN KEY (ProtocolSiteId)
        REFERENCES ProtocolSites(ProtocolSiteId)
);
GO

CREATE TABLE Visits (
    VisitId      UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    EnrollmentId UNIQUEIDENTIFIER  NOT NULL,
    VisitName    NVARCHAR(200)     NOT NULL,
    VisitDate    DATETIME2(0)      NOT NULL,
    VisitStatus  NVARCHAR(50)      NOT NULL  DEFAULT 'SCHEDULED',

    CONSTRAINT FK_Visits_EnrollmentId FOREIGN KEY (EnrollmentId)
        REFERENCES PatientEnrollments(EnrollmentId) ON DELETE CASCADE
);
GO

-- ============================================================
--  MODULE 3 — Sample & Laboratory Management
-- ============================================================

CREATE TABLE Samples (
    SampleId          UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    EnrollmentId      UNIQUEIDENTIFIER  NOT NULL,
    CollectedByUserId UNIQUEIDENTIFIER  NOT NULL,
    SampleType        NVARCHAR(100)     NOT NULL,
    CollectedDate     DATETIME2(0)      NOT NULL,
    Status            NVARCHAR(50)      NOT NULL  DEFAULT 'COLLECTED',

    CONSTRAINT FK_Samples_EnrollmentId      FOREIGN KEY (EnrollmentId)
        REFERENCES PatientEnrollments(EnrollmentId),
    CONSTRAINT FK_Samples_CollectedByUserId FOREIGN KEY (CollectedByUserId)
        REFERENCES Users(UserId)
);
GO

CREATE TABLE LabResults (
    ResultId         UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    SampleId         UNIQUEIDENTIFIER  NOT NULL,
    RecordedByUserId UNIQUEIDENTIFIER  NOT NULL,
    TestType         NVARCHAR(100)     NOT NULL,
    ResultValue      NVARCHAR(255)     NOT NULL,
    ResultDate       DATETIME2(0)      NOT NULL,

    CONSTRAINT FK_LabResults_SampleId         FOREIGN KEY (SampleId)
        REFERENCES Samples(SampleId) ON DELETE CASCADE,
    CONSTRAINT FK_LabResults_RecordedByUserId FOREIGN KEY (RecordedByUserId)
        REFERENCES Users(UserId)
);
GO

-- ============================================================
--  MODULE 5 — Compliance Management
-- ============================================================

CREATE TABLE ComplianceReports (
    ReportId          UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    ProtocolId        UNIQUEIDENTIFIER  NOT NULL,
    GeneratedByUserId UNIQUEIDENTIFIER  NOT NULL,
    Scope             NVARCHAR(100)     NOT NULL,
    Metrics           NVARCHAR(MAX)     NULL,
    Status            NVARCHAR(50)      NOT NULL  DEFAULT 'DRAFT',
    GeneratedAt       DATETIME2(0)      NOT NULL  DEFAULT GETUTCDATE(),

    CONSTRAINT FK_ComplianceReports_ProtocolId        FOREIGN KEY (ProtocolId)
        REFERENCES Protocols(ProtocolId),
    CONSTRAINT FK_ComplianceReports_GeneratedByUserId FOREIGN KEY (GeneratedByUserId)
        REFERENCES Users(UserId)
);
GO

-- ============================================================
--  MODULE 6 — Analytics & Reporting
-- ============================================================

CREATE TABLE KpiReports (
    ReportId          UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    ProtocolId        UNIQUEIDENTIFIER  NULL,
    GeneratedByUserId UNIQUEIDENTIFIER  NOT NULL,
    Scope             NVARCHAR(100)     NOT NULL,
    Metrics           NVARCHAR(MAX)     NULL,
    GeneratedAt       DATETIME2(0)      NOT NULL  DEFAULT GETUTCDATE(),

    CONSTRAINT FK_KpiReports_ProtocolId        FOREIGN KEY (ProtocolId)
        REFERENCES Protocols(ProtocolId),
    CONSTRAINT FK_KpiReports_GeneratedByUserId FOREIGN KEY (GeneratedByUserId)
        REFERENCES Users(UserId)
);
GO

-- ============================================================
--  MODULE 7 — Notifications & Alerts
-- ============================================================

CREATE TABLE Notifications (
    NotificationId UNIQUEIDENTIFIER  NOT NULL  DEFAULT NEWSEQUENTIALID()  PRIMARY KEY,
    UserId         UNIQUEIDENTIFIER  NOT NULL,
    Category       NVARCHAR(100)     NOT NULL,
    Message        NVARCHAR(MAX)     NOT NULL,
    Channel        NVARCHAR(50)      NOT NULL  DEFAULT 'IN_APP',
    Status         NVARCHAR(50)      NOT NULL  DEFAULT 'UNREAD',
    CreatedAt      DATETIME2(0)      NOT NULL  DEFAULT GETUTCDATE(),
    ReadAt         DATETIME2(0)      NULL,

    CONSTRAINT FK_Notifications_UserId FOREIGN KEY (UserId)
        REFERENCES Users(UserId) ON DELETE CASCADE
);
GO

-- ============================================================
--  INDEXES
-- ============================================================

-- Module 1
CREATE NONCLUSTERED INDEX IX_Users_Email             ON Users(Email);
CREATE NONCLUSTERED INDEX IX_Users_RoleId            ON Users(RoleId);
CREATE NONCLUSTERED INDEX IX_UserSessions_UserId     ON UserSessions(UserId);
CREATE NONCLUSTERED INDEX IX_UserSessions_TokenJti   ON UserSessions(TokenJti);
CREATE NONCLUSTERED INDEX IX_UserSessions_ExpiresAt  ON UserSessions(ExpiresAt);

-- Module 2
CREATE NONCLUSTERED INDEX IX_Protocols_Status        ON Protocols(Status);
CREATE NONCLUSTERED INDEX IX_Protocols_CreatedBy     ON Protocols(CreatedByUserId);
CREATE NONCLUSTERED INDEX IX_ProtocolSites_Protocol  ON ProtocolSites(ProtocolId);
CREATE NONCLUSTERED INDEX IX_ProtocolSites_Site      ON ProtocolSites(SiteId);

-- Module 3
CREATE NONCLUSTERED INDEX IX_Samples_EnrollmentId    ON Samples(EnrollmentId);
CREATE NONCLUSTERED INDEX IX_Samples_Status          ON Samples(Status);
CREATE NONCLUSTERED INDEX IX_Samples_CollectedDate   ON Samples(CollectedDate DESC);
CREATE NONCLUSTERED INDEX IX_LabResults_SampleId     ON LabResults(SampleId);

-- Module 4
CREATE NONCLUSTERED INDEX IX_Patients_Status         ON Patients(PatientStatus);
CREATE NONCLUSTERED INDEX IX_Patients_Name           ON Patients(Name);
CREATE NONCLUSTERED INDEX IX_Enrollments_PatientId   ON PatientEnrollments(PatientId);
CREATE NONCLUSTERED INDEX IX_Enrollments_SiteId      ON PatientEnrollments(ProtocolSiteId);
CREATE NONCLUSTERED INDEX IX_Visits_EnrollmentId     ON Visits(EnrollmentId);
CREATE NONCLUSTERED INDEX IX_Visits_VisitDate        ON Visits(VisitDate DESC);
CREATE NONCLUSTERED INDEX IX_Visits_VisitStatus      ON Visits(VisitStatus);

-- Module 5 & 6
CREATE NONCLUSTERED INDEX IX_ComplianceReports_Protocol ON ComplianceReports(ProtocolId);
CREATE NONCLUSTERED INDEX IX_KpiReports_Protocol        ON KpiReports(ProtocolId);

-- Module 7
CREATE NONCLUSTERED INDEX IX_Notifications_UserId    ON Notifications(UserId);
CREATE NONCLUSTERED INDEX IX_Notifications_Status    ON Notifications(Status);
CREATE NONCLUSTERED INDEX IX_Notifications_CreatedAt ON Notifications(CreatedAt DESC);
GO

PRINT 'LifeSci360_Services schema created successfully.';
GO
