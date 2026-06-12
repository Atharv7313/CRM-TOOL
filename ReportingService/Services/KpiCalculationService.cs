using Microsoft.EntityFrameworkCore;
using ReportingService.Data;
using ReportingService.Data.Entities;
using ReportingService.Interfaces;
using Shared.DTOs;

namespace ReportingService.Services;

/// <summary>
/// Calculates KPI values from real data in the database.
///
/// EnrollmentRate       = (Active enrollments / Target) × 100
/// SampleProcessingRate = (Processed samples / Total collected) × 100
/// ComplianceScore      = (APPROVED compliance reports / Total) × 100
/// SitePerformanceScore = (ACTIVE protocol sites / Total sites) × 100
/// </summary>
public class KpiCalculationService(ServicesDbContext db) : IKpiCalculationService
{
    private static readonly DateTime _thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
    private static readonly DateTime _sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);

    public async Task<KpiMetrics> CalculateCurrentAsync(Guid? protocolId) =>
        await CalculateForPeriodAsync(protocolId, _thirtyDaysAgo, DateTime.UtcNow);

    public async Task<KpiMetrics> CalculatePreviousPeriodAsync(Guid? protocolId) =>
        await CalculateForPeriodAsync(protocolId, _sixtyDaysAgo, _thirtyDaysAgo);

    private async Task<KpiMetrics> CalculateForPeriodAsync(
        Guid? protocolId, DateTime from, DateTime to)
    {
        return new KpiMetrics(
            EnrollmentRate: await CalcEnrollmentRateAsync(protocolId, from, to),
            SampleProcessingRate: await CalcSampleProcessingRateAsync(protocolId, from, to),
            ComplianceScore: await CalcComplianceScoreAsync(protocolId, from, to),
            SitePerformanceScore: await CalcSitePerformanceScoreAsync(protocolId)
        );
    }

    // ── Enrollment Rate ───────────────────────────────────────────────────────
    // Active enrollments as a % of all patients registered
    private async Task<double> CalcEnrollmentRateAsync(
        Guid? protocolId, DateTime from, DateTime to)
    {
        IQueryable<Patient> patientQuery =
            db.Patients.Where(p => p.PatientStatus != "DELETED");

        if (protocolId.HasValue)
            patientQuery = patientQuery.Where(p =>
                p.PatientEnrollments.Any(e =>
                    e.ProtocolSite != null &&
                    e.ProtocolSite.ProtocolId == protocolId.Value));

        var totalPatients = await patientQuery.CountAsync();

        var enrollQuery = db.PatientEnrollments
            .Where(e => e.EnrollmentStatus == "ACTIVE");

        if (protocolId.HasValue)
            enrollQuery = enrollQuery.Where(e =>
                e.ProtocolSite != null &&
                e.ProtocolSite.ProtocolId == protocolId.Value);

        var activeEnrollments = await enrollQuery.CountAsync();

        return totalPatients == 0 ? 0 : Math.Round((double)activeEnrollments / totalPatients * 100, 2);
    }

    // ── Sample Processing Rate ────────────────────────────────────────────────
    // TESTED or ANALYZED samples as % of all collected samples
    private async Task<double> CalcSampleProcessingRateAsync(
        Guid? protocolId, DateTime from, DateTime to)
    {
        var sampleQuery = db.Samples.AsQueryable();

        if (protocolId.HasValue)
            sampleQuery = sampleQuery.Where(s =>
                s.Enrollment != null &&
                s.Enrollment.ProtocolSite != null &&
                s.Enrollment.ProtocolSite.ProtocolId == protocolId.Value);

        var total = await sampleQuery.CountAsync();
        var processed = await sampleQuery.CountAsync(s =>
            s.Status == "TESTED" || s.Status == "ANALYZED");

        return total == 0 ? 0 : Math.Round((double)processed / total * 100, 2);
    }

    // ── Compliance Score ──────────────────────────────────────────────────────
    // Completed visits / All due visits (past VisitDate, excluding CANCELLED) × 100
    private async Task<double> CalcComplianceScoreAsync(
        Guid? protocolId, DateTime from, DateTime to)
    {
        var now = DateTime.UtcNow;

        var query = db.Visits
            .Where(v => v.VisitDate <= now && v.VisitStatus != "CANCELLED");

        if (protocolId.HasValue)
            query = query.Where(v =>
                v.Enrollment.ProtocolSite != null &&
                v.Enrollment.ProtocolSite.ProtocolId == protocolId.Value);

        var due       = await query.CountAsync();
        var completed = await query.CountAsync(v => v.VisitStatus == "COMPLETED");

        return due == 0 ? 100 : Math.Round((double)completed / due * 100, 2);
    }

    // ── Site Performance Score ────────────────────────────────────────────────
    // ACTIVE protocol sites as % of all sites
    private async Task<double> CalcSitePerformanceScoreAsync(Guid? protocolId)
    {
        var query = db.ProtocolSites.AsQueryable();

        if (protocolId.HasValue)
            query = query.Where(ps => ps.ProtocolId == protocolId.Value);

        var total = await query.CountAsync();
        var active = await query.CountAsync(ps => ps.Status == "ACTIVE");

        return total == 0 ? 0 : Math.Round((double)active / total * 100, 2);
    }
}