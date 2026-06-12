using PatientService.Controllers;
using Shared.DTOs;

namespace PatientService.Services;

public interface IVisitService
{
    Task<object> GetByEnrollmentAsync(Guid enrollmentId);
    Task<IEnumerable<VisitDto>> GetFilteredAsync(
        DateTime? date, Guid? protocolSiteId, string? status);
    Task<(bool Success, string? Error, VisitDto? Data)> AddAsync(
        Guid enrollmentId, string visitName, DateTime visitDate);
    Task<(bool Success, string? Error, VisitDto? Data)> RescheduleAsync(
        Guid id, DateTime newDate);
    Task<(bool Success, string? Error)> CancelAsync(Guid id);
    Task<string> SubmitReviewAsync(
        DateTime date, Guid? protocolSiteId, List<Guid> attendedVisitIds);

    // ✅ NEW
    Task<(bool Success, string? Error, int Count)> BulkScheduleAsync(
        Guid protocolId, List<BulkVisitItem> visits);
}