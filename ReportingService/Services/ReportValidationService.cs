using Microsoft.EntityFrameworkCore;
using ReportingService.Data;
using ReportingService.Interfaces;
using Shared.DTOs;

namespace ReportingService.Services;

public class ReportValidationService(ServicesDbContext db) : IReportValidationService
{
    public async Task ValidateCreateAsync(CreateKpiReportDto dto)
    {
        // Rule 1: GeneratedByUserId must exist and be active
        var userExists = await db.Users
            .AnyAsync(u => u.UserId == dto.GeneratedByUserId && u.IsActive == true);
        if (!userExists)
            throw new ValidationException(
                $"User {dto.GeneratedByUserId} does not exist or is inactive.");

        // Rule 2: If ProtocolId is provided, it must exist
        if (dto.ProtocolId.HasValue)
        {
            var protocol = await db.Protocols
                .FirstOrDefaultAsync(p => p.ProtocolId == dto.ProtocolId.Value);

            if (protocol is null)
                throw new ValidationException(
                    $"Protocol {dto.ProtocolId} does not exist.");

            // Rule 3: Cannot generate a report for a DRAFT protocol
            if (protocol.Status == "DRAFT")
                throw new ValidationException(
                    $"Cannot generate a KPI report for Protocol '{protocol.Title}' " +
                    $"because it is still in DRAFT status. Activate the protocol first.");

            // Rule 4: No duplicate report for same Protocol + Scope on the same day
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var scopeName = dto.Scope.ToString();

            var duplicateExists = await db.KpiReports.AnyAsync(r =>
                r.ProtocolId == dto.ProtocolId.Value &&
                r.Scope == scopeName &&
                r.GeneratedAt >= today &&
                r.GeneratedAt < tomorrow);

            if (duplicateExists)
                throw new ValidationException(
                    $"A '{scopeName}' KPI report for this protocol has already been " +
                    $"generated today. Only one report per protocol per scope per day is allowed.");
        }
    }
}

public class ValidationException(string message) : Exception(message);