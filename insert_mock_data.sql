-- ============================================================
-- LifeSci360 — Complete Mock Data Insert
-- Run in SSMS connected to (localdb)\MSSQLLocalDB
-- ============================================================

USE LifeSci360_Services;
GO

-- ── Step 0: Get existing IDs we need ────────────────────────
-- Run this first to see available users and protocol sites
SELECT UserId, Name, Email = (SELECT u2.Email FROM Users u2 WHERE u2.UserId = u.UserId)
FROM Users u
INNER JOIN Roles r ON u.RoleId = r.RoleId
WHERE r.RoleName IN ('LAB_TECHNICIAN', 'RESEARCH_SCIENTIST', 'ADMIN', 'SYSTEM_ADMIN');

SELECT ProtocolSiteId, ProtocolId, SiteId FROM ProtocolSites;
GO

-- ── Step 1: Declare User IDs (update with actual values from Step 0) ──
DECLARE @LabTech1   UNIQUEIDENTIFIER = (SELECT TOP 1 u.UserId FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE r.RoleName = 'LAB_TECHNICIAN' ORDER BY u.CreatedAt ASC);
DECLARE @LabTech2   UNIQUEIDENTIFIER = (SELECT TOP 1 u.UserId FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE r.RoleName = 'LAB_TECHNICIAN' ORDER BY u.CreatedAt DESC);
DECLARE @Scientist1 UNIQUEIDENTIFIER = (SELECT TOP 1 u.UserId FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE r.RoleName = 'RESEARCH_SCIENTIST' ORDER BY u.CreatedAt ASC);
DECLARE @Admin      UNIQUEIDENTIFIER = (SELECT TOP 1 u.UserId FROM Users u JOIN Roles r ON u.RoleId = r.RoleName WHERE r.RoleName = 'SYSTEM_ADMIN');
DECLARE @ProtocolSiteId UNIQUEIDENTIFIER = (SELECT TOP 1 ProtocolSiteId FROM ProtocolSites);

PRINT 'LabTech1: '   + CAST(@LabTech1   AS NVARCHAR(50));
PRINT 'LabTech2: '   + CAST(@LabTech2   AS NVARCHAR(50));
PRINT 'Scientist1: ' + CAST(@Scientist1 AS NVARCHAR(50));
PRINT 'ProtocolSite: ' + CAST(@ProtocolSiteId AS NVARCHAR(50));
GO

-- ── Step 2: Insert Mock Patients ────────────────────────────
INSERT INTO Patients (PatientId, Name, DateOfBirth, ContactInfo, PatientStatus, CreatedAt)
VALUES
    (NEWID(), 'Rajesh Kumar',    '1978-03-15', 'rajesh.kumar@gmail.com',   'ACTIVE', GETUTCDATE()),
    (NEWID(), 'Preethi Nair',    '1965-07-22', 'preethi.nair@gmail.com',   'ACTIVE', GETUTCDATE()),
    (NEWID(), 'Suresh Menon',    '1982-11-08', 'suresh.menon@gmail.com',   'ACTIVE', GETUTCDATE()),
    (NEWID(), 'Ananya Sharma',   '1990-04-30', 'ananya.sharma@gmail.com',  'ACTIVE', GETUTCDATE()),
    (NEWID(), 'Karthik Rajan',   '1975-01-12', 'karthik.rajan@gmail.com',  'ACTIVE', GETUTCDATE());
GO

PRINT 'Patients inserted ✅';
GO

-- ── Step 3: Enroll Patients in Protocol ─────────────────────
DECLARE @ProtocolSiteId UNIQUEIDENTIFIER = (SELECT TOP 1 ProtocolSiteId FROM ProtocolSites);

INSERT INTO PatientEnrollments (EnrollmentId, PatientId, ProtocolSiteId, EnrollmentStatus, EnrolledAt)
SELECT
    NEWID(),
    p.PatientId,
    @ProtocolSiteId,
    'ACTIVE',
    GETUTCDATE()
FROM Patients p
WHERE p.ContactInfo IN (
    'rajesh.kumar@gmail.com',
    'preethi.nair@gmail.com',
    'suresh.menon@gmail.com',
    'ananya.sharma@gmail.com',
    'karthik.rajan@gmail.com'
);
GO

PRINT 'Enrollments inserted ✅';
GO

-- ── Step 4: Insert Mock Samples ─────────────────────────────
DECLARE @LabTech1 UNIQUEIDENTIFIER = (SELECT TOP 1 u.UserId FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE r.RoleName = 'LAB_TECHNICIAN' ORDER BY u.CreatedAt ASC);
DECLARE @LabTech2 UNIQUEIDENTIFIER = (SELECT TOP 1 u.UserId FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE r.RoleName = 'LAB_TECHNICIAN' ORDER BY u.CreatedAt DESC);
DECLARE @Admin    UNIQUEIDENTIFIER = (SELECT TOP 1 u.UserId FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE r.RoleName = 'SYSTEM_ADMIN');

-- Rajesh Kumar samples
DECLARE @E_Rajesh   UNIQUEIDENTIFIER = (SELECT TOP 1 pe.EnrollmentId FROM PatientEnrollments pe JOIN Patients p ON pe.PatientId = p.PatientId WHERE p.ContactInfo = 'rajesh.kumar@gmail.com');
DECLARE @E_Preethi  UNIQUEIDENTIFIER = (SELECT TOP 1 pe.EnrollmentId FROM PatientEnrollments pe JOIN Patients p ON pe.PatientId = p.PatientId WHERE p.ContactInfo = 'preethi.nair@gmail.com');
DECLARE @E_Suresh   UNIQUEIDENTIFIER = (SELECT TOP 1 pe.EnrollmentId FROM PatientEnrollments pe JOIN Patients p ON pe.PatientId = p.PatientId WHERE p.ContactInfo = 'suresh.menon@gmail.com');
DECLARE @E_Ananya   UNIQUEIDENTIFIER = (SELECT TOP 1 pe.EnrollmentId FROM PatientEnrollments pe JOIN Patients p ON pe.PatientId = p.PatientId WHERE p.ContactInfo = 'ananya.sharma@gmail.com');
DECLARE @E_Karthik  UNIQUEIDENTIFIER = (SELECT TOP 1 pe.EnrollmentId FROM PatientEnrollments pe JOIN Patients p ON pe.PatientId = p.PatientId WHERE p.ContactInfo = 'karthik.rajan@gmail.com');

-- Declare sample IDs for lab results later
DECLARE @S1 UNIQUEIDENTIFIER = NEWID();
DECLARE @S2 UNIQUEIDENTIFIER = NEWID();
DECLARE @S3 UNIQUEIDENTIFIER = NEWID();
DECLARE @S4 UNIQUEIDENTIFIER = NEWID();
DECLARE @S5 UNIQUEIDENTIFIER = NEWID();
DECLARE @S6 UNIQUEIDENTIFIER = NEWID();
DECLARE @S7 UNIQUEIDENTIFIER = NEWID();

INSERT INTO Samples (SampleId, EnrollmentId, CollectedByUserId, SampleType, CollectedDate, Status, Notes)
VALUES
    (@S1, @E_Rajesh,  @LabTech1, 'Blood',   '2026-06-01 09:00', 'TESTED',    NULL),
    (@S2, @E_Rajesh,  @LabTech1, 'Urine',   '2026-06-01 09:15', 'COLLECTED', NULL),
    (@S3, @E_Preethi, @LabTech2, 'Plasma',  '2026-06-02 10:00', 'ANALYZED',  NULL),
    (@S4, @E_Preethi, @LabTech2, 'Serum',   '2026-06-02 10:30', 'TESTED',    NULL),
    (@S5, @E_Suresh,  @LabTech1, 'Blood',   '2026-06-03 08:30', 'TESTED',    NULL),
    (@S6, @E_Ananya,  @LabTech2, 'Tissue',  '2026-06-04 11:00', 'COLLECTED', NULL),
    (@S7, @E_Karthik, @LabTech1, 'Saliva',  '2026-06-05 09:45', 'ANALYZED',  NULL);
GO

PRINT 'Samples inserted ✅';
GO

-- ── Step 5: Insert Mock Lab Results ─────────────────────────
DECLARE @LabTech1  UNIQUEIDENTIFIER = (SELECT TOP 1 u.UserId FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE r.RoleName = 'LAB_TECHNICIAN' ORDER BY u.CreatedAt ASC);
DECLARE @LabTech2  UNIQUEIDENTIFIER = (SELECT TOP 1 u.UserId FROM Users u JOIN Roles r ON u.RoleId = r.RoleId WHERE r.RoleName = 'LAB_TECHNICIAN' ORDER BY u.CreatedAt DESC);

-- Get sample IDs
DECLARE @S1 UNIQUEIDENTIFIER = (SELECT TOP 1 SampleId FROM Samples WHERE SampleType = 'Blood'  AND CollectedDate = '2026-06-01 09:00');
DECLARE @S3 UNIQUEIDENTIFIER = (SELECT TOP 1 SampleId FROM Samples WHERE SampleType = 'Plasma' AND CollectedDate = '2026-06-02 10:00');
DECLARE @S4 UNIQUEIDENTIFIER = (SELECT TOP 1 SampleId FROM Samples WHERE SampleType = 'Serum'  AND CollectedDate = '2026-06-02 10:30');
DECLARE @S5 UNIQUEIDENTIFIER = (SELECT TOP 1 SampleId FROM Samples WHERE SampleType = 'Blood'  AND CollectedDate = '2026-06-03 08:30');
DECLARE @S7 UNIQUEIDENTIFIER = (SELECT TOP 1 SampleId FROM Samples WHERE SampleType = 'Saliva' AND CollectedDate = '2026-06-05 09:45');

INSERT INTO LabResults (ResultId, SampleId, RecordedByUserId, TestType, ResultValue, ResultDate)
VALUES
    (NEWID(), @S1, @LabTech1, 'CBC',           '13.2 g/dL',   '2026-06-01'),
    (NEWID(), @S1, @LabTech1, 'Blood Glucose',  '92 mg/dL',    '2026-06-01'),
    (NEWID(), @S3, @LabTech2, 'HbA1c',          '7.1%',        '2026-06-02'),
    (NEWID(), @S3, @LabTech2, 'Cholesterol',    '195 mg/dL',   '2026-06-02'),
    (NEWID(), @S4, @LabTech2, 'Liver Function', 'Normal',      '2026-06-02'),
    (NEWID(), @S5, @LabTech1, 'WBC Count',      '7500 /uL',    '2026-06-03'),
    (NEWID(), @S5, @LabTech1, 'Platelet',       '250000 /uL',  '2026-06-03'),
    (NEWID(), @S7, @LabTech1, 'Cortisol',       '18 mcg/dL',   '2026-06-05'),
    (NEWID(), @S7, @LabTech1, 'Amylase',        '65 U/L',      '2026-06-05');
GO

PRINT 'Lab Results inserted ✅';
GO

-- ── Step 6: Verify All Inserted Data ─────────────────────────
SELECT 'Patients'         AS TableName, COUNT(*) AS Count FROM Patients
UNION ALL
SELECT 'PatientEnrollments',            COUNT(*) FROM PatientEnrollments
UNION ALL
SELECT 'Samples',                       COUNT(*) FROM Samples
UNION ALL
SELECT 'LabResults',                    COUNT(*) FROM LabResults;
GO

PRINT '==============================';
PRINT 'All mock data inserted! ✅';
PRINT 'Refresh browser to see data.';
PRINT '==============================';
GO
