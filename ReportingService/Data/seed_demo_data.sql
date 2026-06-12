-- Demo data for the Analytics & Reporting dashboard.
-- Produces varied, non-zero KPIs:
--   EnrollmentRate       current 80%  / previous 50%   (trend +30)
--   SampleProcessingRate 60%          (no period filter in backend)
--   ComplianceScore      current 75%  / previous 33.3% (trend +41.7)
--   SitePerformanceScore 75%          (no period filter in backend)
-- Idempotent: only seeds when no Protocols exist yet.

SET NOCOUNT ON;

IF (SELECT COUNT(*) FROM dbo.Protocols) > 0
BEGIN
    PRINT 'Demo data already present (Protocols table not empty) — skipping.';
    RETURN;
END;

DECLARE @uid uniqueidentifier = (SELECT TOP 1 UserId FROM dbo.Users WHERE Email = N'admin@lifesci360.com');
IF @uid IS NULL SET @uid = (SELECT TOP 1 UserId FROM dbo.Users);
IF @uid IS NULL BEGIN PRINT 'No users exist — seed an admin first.'; RETURN; END;

BEGIN TRANSACTION;

-- ── Sites ──────────────────────────────────────────────────────────────────
DECLARE @site1 uniqueidentifier = NEWID(), @site2 uniqueidentifier = NEWID(), @site3 uniqueidentifier = NEWID();
INSERT INTO dbo.Sites (SiteId, Name, Location) VALUES
    (@site1, N'Boston Clinical Center',     N'Boston, MA'),
    (@site2, N'Bay Area Research Institute', N'San Francisco, CA'),
    (@site3, N'Midwest Trials Unit',         N'Chicago, IL');

-- ── Protocols (3 ACTIVE + 1 DRAFT → ActiveProtocols = 3) ───────────────────
DECLARE @p1 uniqueidentifier = NEWID(), @p2 uniqueidentifier = NEWID(),
        @p3 uniqueidentifier = NEWID(), @p4 uniqueidentifier = NEWID();
INSERT INTO dbo.Protocols (ProtocolId, Title, Phase, Status, Description, StartDate, CreatedByUserId) VALUES
    (@p1, N'ONC-204 Solid Tumor Study',      N'Phase II',  N'ACTIVE', N'Oncology dose-escalation', DATEADD(day,-120,getutcdate()), @uid),
    (@p2, N'CARD-118 Heart Failure Trial',   N'Phase III', N'ACTIVE', N'Cardiovascular outcomes',  DATEADD(day,-90, getutcdate()), @uid),
    (@p3, N'NEU-330 Alzheimer Cohort',       N'Phase II',  N'ACTIVE', N'Neurology biomarker study', DATEADD(day,-60, getutcdate()), @uid),
    (@p4, N'IMM-009 Vaccine Pilot',          N'Phase I',   N'DRAFT',  N'Immunology pilot',          NULL,                           @uid);

-- ── ProtocolSites (3 ACTIVE + 1 INACTIVE → SitePerformance = 75%) ──────────
DECLARE @ps1 uniqueidentifier = NEWID(), @ps2 uniqueidentifier = NEWID(),
        @ps3 uniqueidentifier = NEWID(), @ps4 uniqueidentifier = NEWID();
INSERT INTO dbo.ProtocolSites (ProtocolSiteId, ProtocolId, SiteId, InvestigatorUserId, Status) VALUES
    (@ps1, @p1, @site1, @uid, N'ACTIVE'),
    (@ps2, @p2, @site2, @uid, N'ACTIVE'),
    (@ps3, @p3, @site3, @uid, N'ACTIVE'),
    (@ps4, @p1, @site2, @uid, N'INACTIVE');

-- ── Patients ───────────────────────────────────────────────────────────────
DECLARE @pat1 uniqueidentifier=NEWID(), @pat2 uniqueidentifier=NEWID(), @pat3 uniqueidentifier=NEWID(),
        @pat4 uniqueidentifier=NEWID(), @pat5 uniqueidentifier=NEWID(), @pat6 uniqueidentifier=NEWID(),
        @pat7 uniqueidentifier=NEWID(), @pat8 uniqueidentifier=NEWID(), @pat9 uniqueidentifier=NEWID();
INSERT INTO dbo.Patients (PatientId, Name, DateOfBirth, ContactInfo, PatientStatus) VALUES
    (@pat1, N'John Carter',      '1985-04-12', N'john.carter@example.com',   N'ACTIVE'),
    (@pat2, N'Maria Lopez',      '1978-11-03', N'maria.lopez@example.com',   N'ACTIVE'),
    (@pat3, N'Wei Chen',         '1990-07-21', N'wei.chen@example.com',      N'ACTIVE'),
    (@pat4, N'Aisha Khan',       '1982-01-30', N'aisha.khan@example.com',    N'ACTIVE'),
    (@pat5, N'Liam Murphy',      '1995-09-09', N'liam.murphy@example.com',   N'ACTIVE'),
    (@pat6, N'Sofia Rossi',      '1969-03-17', N'sofia.rossi@example.com',   N'ACTIVE'),
    (@pat7, N'David Okoro',      '1988-12-25', N'david.okoro@example.com',   N'ACTIVE'),
    (@pat8, N'Emma Johansson',   '1974-06-05', N'emma.j@example.com',        N'ACTIVE'),
    (@pat9, N'Noah Williams',    '1992-02-14', N'noah.williams@example.com', N'ACTIVE');

-- ── Enrollments ────────────────────────────────────────────────────────────
-- Current period (last 30d): 5 total, 4 ACTIVE, 1 WITHDRAWN → 80%
-- Previous period (30-60d):  4 total, 2 ACTIVE, 2 WITHDRAWN → 50%
DECLARE @e1 uniqueidentifier=NEWID(), @e2 uniqueidentifier=NEWID(), @e3 uniqueidentifier=NEWID(),
        @e4 uniqueidentifier=NEWID(), @e5 uniqueidentifier=NEWID();
DECLARE @cur datetime2(0) = DATEADD(day,-10,getutcdate());
DECLARE @prev datetime2(0) = DATEADD(day,-45,getutcdate());
INSERT INTO dbo.PatientEnrollments (EnrollmentId, PatientId, ProtocolSiteId, EnrollmentStatus, EnrolledAt) VALUES
    (@e1, @pat1, @ps1, N'ACTIVE',    @cur),
    (@e2, @pat2, @ps2, N'ACTIVE',    @cur),
    (@e3, @pat3, @ps3, N'ACTIVE',    @cur),
    (@e4, @pat4, @ps1, N'ACTIVE',    @cur),
    (@e5, @pat5, @ps2, N'WITHDRAWN', @cur),
    (NEWID(), @pat6, @ps3, N'ACTIVE',    @prev),
    (NEWID(), @pat7, @ps1, N'ACTIVE',    @prev),
    (NEWID(), @pat8, @ps2, N'WITHDRAWN', @prev),
    (NEWID(), @pat9, @ps3, N'WITHDRAWN', @prev);

-- ── Samples (5 total, 3 processed → SampleProcessingRate = 60%) ────────────
DECLARE @s1 uniqueidentifier=NEWID(), @s2 uniqueidentifier=NEWID(), @s3 uniqueidentifier=NEWID();
INSERT INTO dbo.Samples (SampleId, EnrollmentId, CollectedByUserId, SampleType, CollectedDate, Status) VALUES
    (@s1,     @e1, @uid, N'Blood',  DATEADD(day,-9,getutcdate()), N'PROCESSED'),
    (@s2,     @e2, @uid, N'Plasma', DATEADD(day,-8,getutcdate()), N'PROCESSED'),
    (@s3,     @e3, @uid, N'Tissue', DATEADD(day,-7,getutcdate()), N'PROCESSED'),
    (NEWID(), @e4, @uid, N'Blood',  DATEADD(day,-6,getutcdate()), N'COLLECTED'),
    (NEWID(), @e1, @uid, N'Urine',  DATEADD(day,-5,getutcdate()), N'COLLECTED');

-- ── Lab Results (one per processed sample) ─────────────────────────────────
INSERT INTO dbo.LabResults (ResultId, SampleId, RecordedByUserId, TestType, ResultValue, ResultDate) VALUES
    (NEWID(), @s1, @uid, N'CBC Panel',     N'Normal',        DATEADD(day,-8,getutcdate())),
    (NEWID(), @s2, @uid, N'Lipid Profile', N'Elevated LDL',  DATEADD(day,-7,getutcdate())),
    (NEWID(), @s3, @uid, N'Histopathology',N'Benign',        DATEADD(day,-6,getutcdate()));

-- ── Compliance Reports ──────────────────────────────────────────────────────
-- Current (last 30d): 4 total, 3 APPROVED → 75%
-- Previous (30-60d):  3 total, 1 APPROVED → 33.3%
INSERT INTO dbo.ComplianceReports (ReportId, ProtocolId, GeneratedByUserId, Scope, Metrics, Status, GeneratedAt) VALUES
    (NEWID(), @p1, @uid, N'Compliance', NULL, N'APPROVED', @cur),
    (NEWID(), @p2, @uid, N'Compliance', NULL, N'APPROVED', @cur),
    (NEWID(), @p3, @uid, N'Compliance', NULL, N'APPROVED', @cur),
    (NEWID(), @p1, @uid, N'Compliance', NULL, N'DRAFT',    @cur),
    (NEWID(), @p2, @uid, N'Compliance', NULL, N'APPROVED', @prev),
    (NEWID(), @p3, @uid, N'Compliance', NULL, N'REJECTED', @prev),
    (NEWID(), @p1, @uid, N'Compliance', NULL, N'DRAFT',    @prev);

-- ── KPI Reports (populate the Generated Reports table; TotalReports = 4) ────
INSERT INTO dbo.KpiReports (ReportId, ProtocolId, GeneratedByUserId, Scope, Metrics, GeneratedAt) VALUES
    (NEWID(), @p1,  @uid, N'Enrollment',       N'{"enrollmentRate":80,"sampleProcessingRate":60,"complianceScore":75,"sitePerformanceScore":75}', DATEADD(day,-3,getutcdate())),
    (NEWID(), @p2,  @uid, N'SampleProcessing', N'{"enrollmentRate":80,"sampleProcessingRate":60,"complianceScore":75,"sitePerformanceScore":75}', DATEADD(day,-2,getutcdate())),
    (NEWID(), @p3,  @uid, N'Compliance',       N'{"enrollmentRate":80,"sampleProcessingRate":60,"complianceScore":75,"sitePerformanceScore":75}', DATEADD(day,-1,getutcdate())),
    (NEWID(), NULL, @uid, N'SitePerformance',  N'{"enrollmentRate":80,"sampleProcessingRate":60,"complianceScore":75,"sitePerformanceScore":75}', getutcdate());

COMMIT TRANSACTION;
PRINT 'Demo data seeded successfully.';
