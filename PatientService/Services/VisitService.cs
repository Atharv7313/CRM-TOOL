using PatientService.Data.Entities;
using PatientService.Repository;
using Shared.DTOs;

namespace PatientService.Services;

public class VisitService : IVisitService
{
    private readonly IVisitRepository      _visitRepo;
    private readonly IEnrollmentRepository _enrollRepo;

    public VisitService(
        IVisitRepository      visitRepo,
        IEnrollmentRepository enrollRepo)
    {
        _visitRepo  = visitRepo;
        _enrollRepo = enrollRepo;
    }

    public async Task<object> GetByEnrollmentAsync(Guid enrollmentId)
    {
        var all = (await _visitRepo.GetByEnrollmentIdAsync(enrollmentId))
            .OrderBy(v => v.VisitDate).ToList();

        var upcoming = all
            .Where(v => v.VisitStatus is "SCHEDULED" or "RESCHEDULED")
            .Select(v => MapToDto(v, all)).ToList();

        var history = all
            .Where(v => v.VisitStatus is "COMPLETED" or "MISSED" or "CANCELLED")
            .Select(v => MapToDto(v, all)).ToList();

        return new { Upcoming = upcoming, History = history };
    }

    public async Task<IEnumerable<VisitDto>> GetFilteredAsync(
        DateTime? date, Guid? protocolSiteId, string? status)
    {
        var matchedVisits = (await _visitRepo.GetFilteredAsync(date, protocolSiteId, status?.ToUpper()))
            .ToList();

        if (!matchedVisits.Any())
            return Enumerable.Empty<VisitDto>();

        var enrollmentIds = matchedVisits
            .Select(v => v.EnrollmentId)
            .Distinct()
            .ToList();

        var allSiblings = new Dictionary<Guid, List<Visit>>();
        foreach (var enrollmentId in enrollmentIds)
        {
            var siblings = (await _visitRepo.GetByEnrollmentIdAsync(enrollmentId))
                .OrderBy(v => v.VisitDate)
                .ToList();
            allSiblings[enrollmentId] = siblings;
        }

        return matchedVisits.Select(v =>
            MapToDto(v, allSiblings.TryGetValue(v.EnrollmentId, out var siblings)
                ? siblings
                : null));
    }

    public async Task<(bool Success, string? Error, VisitDto? Data)> AddAsync(
        Guid enrollmentId, string visitName, DateTime visitDate)
    {
        if (string.IsNullOrWhiteSpace(visitName))
            return (false, "Visit name is required.", null);

        if (visitDate.Date < DateTime.UtcNow.Date)
            return (false, "Visit date cannot be in the past.", null);

        var enrollment = await _enrollRepo.GetByIdAsync(enrollmentId);
        if (enrollment is null)
            return (false, "Enrollment not found.", null);

        if (enrollment.EnrollmentStatus != "ACTIVE")
            return (false, "Cannot add visits to a non-active enrollment.", null);

        if (enrollment.Patient?.PatientStatus == "INACTIVE")
            return (false, "Cannot add visits to an inactive patient.", null);

        // ── Validate visit date is within protocol window ────────────────
        var (dateValid, dateError) = await ValidateVisitDateAsync(enrollment, visitDate);
        if (!dateValid)
            return (false, dateError, null);

        // Prevent duplicate: only one visit allowed per date for an enrollment
        var existingVisits = await _visitRepo.GetByEnrollmentIdAsync(enrollmentId);
        bool dateAlreadyBooked = existingVisits.Any(v => v.VisitDate.Date == visitDate.Date);
        if (dateAlreadyBooked)
            return (false,
                $"A visit is already scheduled on {visitDate:dd MMM yyyy} for this patient. Only one visit per date is allowed.",
                null);

        var visit = new Visit
        {
            EnrollmentId = enrollmentId,
            VisitName    = visitName,
            VisitDate    = visitDate,
            VisitStatus  = "SCHEDULED"
        };

        var created = await _visitRepo.CreateAsync(visit);

        var siblings = (await _visitRepo.GetByEnrollmentIdAsync(enrollmentId))
            .OrderBy(v => v.VisitDate).ToList();

        return (true, null, MapToDto(created, siblings));
    }

    public async Task<(bool Success, string? Error, VisitDto? Data)> RescheduleAsync(
        Guid id, DateTime newDate)
    {
        if (newDate.Date < DateTime.UtcNow.Date)
            return (false, "Reschedule date cannot be in the past.", null);

        var visit = await _visitRepo.GetByIdAsync(id);
        if (visit is null)
            return (false, "Visit not found.", null);

        // Only SCHEDULED, RESCHEDULED, and MISSED visits can be rescheduled
        if (visit.VisitStatus is not ("SCHEDULED" or "RESCHEDULED" or "MISSED"))
            return (false,
                $"Cannot reschedule a visit with status '{visit.VisitStatus}'. " +
                "Only Scheduled, Rescheduled, or Missed visits can be rescheduled.", null);

        // Enrollment must still be ACTIVE
        var enrollment = await _enrollRepo.GetByIdAsync(visit.EnrollmentId);
        if (enrollment?.EnrollmentStatus != "ACTIVE")
            return (false, "Cannot reschedule a visit for a non-active enrollment.", null);

        // ── Validate new date is within protocol window ──────────────────
        var (dateValid, dateError) = await ValidateVisitDateAsync(enrollment, newDate);
        if (!dateValid)
            return (false, dateError, null);

        visit.VisitDate   = newDate;
        visit.VisitStatus = "RESCHEDULED";
        await _visitRepo.UpdateAsync(visit);

        var siblings = (await _visitRepo.GetByEnrollmentIdAsync(visit.EnrollmentId))
            .OrderBy(v => v.VisitDate).ToList();

        return (true, null, MapToDto(visit, siblings));
    }

    public async Task<(bool Success, string? Error)> CancelAsync(Guid id)
    {
        var visit = await _visitRepo.GetByIdAsync(id);
        if (visit is null)
            return (false, "Visit not found.");

        // Only SCHEDULED or RESCHEDULED visits can be cancelled
        if (visit.VisitStatus is not ("SCHEDULED" or "RESCHEDULED"))
            return (false,
                $"Cannot cancel a visit with status '{visit.VisitStatus}'. " +
                "Only Scheduled or Rescheduled visits can be removed.");

        visit.VisitStatus = "CANCELLED";
        await _visitRepo.UpdateAsync(visit);

        // ── Auto-update enrollment status after a visit is cancelled ────────
        var enrollment = await _enrollRepo.GetByIdAsync(visit.EnrollmentId);
        if (enrollment?.EnrollmentStatus == "ACTIVE")
        {
            var allVisits = (await _visitRepo.GetByEnrollmentIdAsync(visit.EnrollmentId)).ToList();

            // Only act if no visits remain SCHEDULED or RESCHEDULED
            bool hasUpcoming = allVisits.Any(v => v.VisitStatus is "SCHEDULED" or "RESCHEDULED");

            if (!hasUpcoming && allVisits.Any())
            {
                // COMPLETED only if patient actually attended at least one visit
                bool patientAttended = allVisits.Any(v => v.VisitStatus == "COMPLETED");

                enrollment.EnrollmentStatus = patientAttended
                    ? "COMPLETED"   // attended ≥ 1 visit → trial completed
                    : "WITHDRAWN";  // never attended (all missed/cancelled) → withdrawn

                await _enrollRepo.UpdateAsync(enrollment);
            }
        }
        // ──────────────────────────────────────────────────────────────────────

        return (true, null);
    }

    public async Task<string> SubmitReviewAsync(
        DateTime date, Guid? protocolSiteId, List<Guid> attendedVisitIds)
    {
        // Fetch both SCHEDULED and RESCHEDULED — rescheduled visits must also be markable
        var scheduled   = await _visitRepo.GetFilteredAsync(date, protocolSiteId, "SCHEDULED");
        var rescheduled = await _visitRepo.GetFilteredAsync(date, protocolSiteId, "RESCHEDULED");
        var allVisits   = scheduled.Concat(rescheduled).ToList();

        foreach (var visit in allVisits)
        {
            visit.VisitStatus = attendedVisitIds.Contains(visit.VisitId)
                ? "COMPLETED"
                : "MISSED";
        }

        await _visitRepo.BulkUpdateAsync(allVisits);

        int completed = allVisits.Count(v => v.VisitStatus == "COMPLETED");
        int missed    = allVisits.Count(v => v.VisitStatus == "MISSED");

        var affectedEnrollmentIds = allVisits
            .Select(v => v.EnrollmentId).Distinct().ToList();

        int autoCompleted = 0;

        foreach (var enrollmentId in affectedEnrollmentIds)
        {
            var enrollment = await _enrollRepo.GetByIdAsync(enrollmentId);
            if (enrollment is null || enrollment.EnrollmentStatus != "ACTIVE")
                continue;

            var allEnrollmentVisits = await _visitRepo.GetByEnrollmentIdAsync(enrollmentId);
            bool hasUpcoming = allEnrollmentVisits
                .Any(v => v.VisitStatus is "SCHEDULED" or "RESCHEDULED");

            if (!hasUpcoming)
            {
                // COMPLETED only if patient actually attended at least one visit
                bool patientAttended = allEnrollmentVisits
                    .Any(v => v.VisitStatus == "COMPLETED");

                enrollment.EnrollmentStatus = patientAttended ? "COMPLETED" : "WITHDRAWN";
                await _enrollRepo.UpdateAsync(enrollment);
                autoCompleted++;
            }
        }

        var message = $"{completed} marked COMPLETED · {missed} marked MISSED.";
        if (autoCompleted > 0)
            message += $" · {autoCompleted} enrollment(s) auto-completed.";

        return message;
    }

    public async Task<(bool Success, string? Error, int Count)> BulkScheduleAsync(
        Guid protocolId, List<BulkVisitItem> visits)
    {
        if (visits is null || visits.Count == 0)
            return (false, "No visits provided.", 0);

        var blankName = visits.FirstOrDefault(v => string.IsNullOrWhiteSpace(v.VisitName));
        if (blankName is not null)
            return (false, "Visit name cannot be empty. Please provide a name for each visit.", 0);

        // ── Validate protocol exists ─────────────────────────────────────
        var protocol = await _enrollRepo.GetProtocolByIdAsync(protocolId);
        if (protocol is null)
            return (false, "Protocol not found. Please provide a valid protocol ID.", 0);

        // ── Validate: visits must be scheduled AFTER enrollment window closes ──
        if (protocol.StartDate.HasValue && protocol.EndDate.HasValue)
        {
            var durationDays   = (protocol.EndDate.Value - protocol.StartDate.Value).TotalDays;
            var windowDays     = (int)Math.Floor(durationDays * 0.10);
            var windowEnd      = protocol.StartDate.Value.Date.AddDays(windowDays);
            var earliestVisit  = windowEnd.AddDays(1); // day after window closes

            var tooEarlyVisit = visits.FirstOrDefault(v => v.VisitDate.Date < earliestVisit);
            if (tooEarlyVisit is not null)
                return (false,
                    $"Visit date '{tooEarlyVisit.VisitDate:dd MMM yyyy}' is before the enrollment " +
                    $"window closes on {windowEnd:dd MMM yyyy}. " +
                    "Visits can only be scheduled after the enrollment window closes.",
                    0);
        }

        // ── Fetch active enrollments ─────────────────────────────────────
        var enrollments = (await _enrollRepo.GetActiveByProtocolAsync(protocolId)).ToList();

        if (enrollments.Count == 0)
            return (false, "No active patients found for this protocol.", 0);

        var toCreate = new List<Visit>();

        foreach (var enrollment in enrollments)
        {
            var existingVisits = (await _visitRepo
                .GetByEnrollmentIdAsync(enrollment.EnrollmentId))
                .ToList();

            foreach (var v in visits)
            {
                bool isDuplicate = existingVisits.Any(e =>
                    e.VisitName.Equals(v.VisitName, StringComparison.OrdinalIgnoreCase) &&
                    e.VisitDate.Date == v.VisitDate.Date);

                if (isDuplicate) continue;

                toCreate.Add(new Visit
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    VisitName    = v.VisitName,
                    VisitDate    = v.VisitDate,
                    VisitStatus  = "SCHEDULED"
                });
            }
        }

        if (toCreate.Count == 0)
            return (false, "All visits already exist for all patients.", 0);

        await _visitRepo.BulkCreateAsync(toCreate);
        return (true, null, toCreate.Count);
    }

    // ── Protocol date range validation ───────────────────────
    private async Task<(bool Valid, string? Error)> ValidateVisitDateAsync(
        PatientEnrollment enrollment, DateTime visitDate)
    {
        var protocolSite = await _enrollRepo.GetProtocolSiteByIdAsync(enrollment.ProtocolSiteId);
        if (protocolSite is null)
            return (false, "Protocol site not found.");

        var protocol = await _enrollRepo.GetProtocolByIdAsync(protocolSite.ProtocolId);
        if (protocol is null)
            return (false, "Protocol not found.");

        if (protocol.StartDate.HasValue && protocol.EndDate.HasValue)
        {
            var durationDays  = (protocol.EndDate.Value - protocol.StartDate.Value).TotalDays;
            var windowDays    = (int)Math.Floor(durationDays * 0.10);
            var windowEnd     = protocol.StartDate.Value.Date.AddDays(windowDays);
            var earliestVisit = windowEnd.AddDays(1);

            if (visitDate.Date < earliestVisit)
                return (false,
                    $"Visit date {visitDate:dd MMM yyyy} is within the enrollment window. " +
                    $"Visits can only be scheduled after {windowEnd:dd MMM yyyy}.");

            if (visitDate.Date > protocol.EndDate.Value.Date)
                return (false,
                    $"Visit date {visitDate:dd MMM yyyy} is after the protocol ends on " +
                    $"{protocol.EndDate.Value:dd MMM yyyy}.");
        }

        return (true, null);
    }

    // ── Mapping ──────────────────────────────────────────────

    private static VisitDto MapToDto(Visit v, List<Visit>? all)
    {
        string? prev = null, next = null;
        int visitNumber = 1, totalVisits = 1;

        if (all is not null && all.Count > 0)
        {
            var idx = all.FindIndex(x => x.VisitId == v.VisitId);
            if (idx > 0)              prev = all[idx - 1].VisitName;
            if (idx < all.Count - 1) next = all[idx + 1].VisitName;

            visitNumber = idx >= 0 ? idx + 1 : 1;
            totalVisits = all.Count;
        }

        // Calculate enrollment window end for frontend date picker restriction
        var protocol = v.Enrollment?.ProtocolSite?.Protocol;
        DateTime? windowEnd = null;
        if (protocol?.StartDate is not null && protocol?.EndDate is not null)
        {
            var durationDays = (protocol.EndDate.Value - protocol.StartDate.Value).TotalDays;
            var windowDays   = (int)Math.Floor(durationDays * 0.10);
            windowEnd        = protocol.StartDate.Value.Date.AddDays(windowDays + 1); // first valid day
        }

        return new VisitDto
        {
            VisitId               = v.VisitId,
            EnrollmentId          = v.EnrollmentId,
            VisitName             = v.VisitName,
            VisitDate             = v.VisitDate,
            VisitStatus           = v.VisitStatus,
            PrevVisit             = prev,
            NextVisit             = next,
            PatientName           = v.Enrollment?.Patient?.Name,
            ProtocolTitle         = v.Enrollment?.ProtocolSite?.Protocol?.Title,
            SiteName              = v.Enrollment?.ProtocolSite?.Site?.Name,
            EnrollmentStatus      = v.Enrollment?.EnrollmentStatus,
            VisitNumber           = visitNumber,
            TotalVisits           = totalVisits,
            ProtocolStartDate     = protocol?.StartDate,
            ProtocolEndDate       = protocol?.EndDate,
            EnrollmentWindowEnd   = windowEnd
        };
    }
}
