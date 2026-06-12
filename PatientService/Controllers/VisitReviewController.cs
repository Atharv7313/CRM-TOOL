using Microsoft.AspNetCore.Mvc;
using PatientService.Services;
using Shared.CL;
using Shared.DTOs;

namespace PatientService.Controllers;

[ApiController]
[Route("api/visit-review")]
public class VisitReviewController : ControllerBase
{
    private readonly IVisitService _service;

    public VisitReviewController(IVisitService service)
    {
        _service = service;
    }

    [HttpPost("submit")]
    public async Task<ActionResult<ApiResponse<string>>> Submit(
        [FromBody] VisitReviewSubmitDto req)
    {
        var message = await _service.SubmitReviewAsync(
            req.Date, req.ProtocolSiteId, req.AttendedVisitIds);

        return Ok(ApiResponse<string>.Success("Saved", message));
    }
}
