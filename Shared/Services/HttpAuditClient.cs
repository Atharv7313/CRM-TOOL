using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Shared.CL.DTOs;

namespace Shared.CL.Services;

public sealed class HttpAuditClient : IAuditClient
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpAuditClient> _logger;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public HttpAuditClient(HttpClient http, ILogger<HttpAuditClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public void Log(AuditLogCreateDto dto)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var body = new StringContent(
                    JsonSerializer.Serialize(dto, _json),
                    Encoding.UTF8,
                    "application/json");
                await _http.PostAsync("api/auditlogs", body).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[AuditClient] Failed to post audit log: {Msg}", ex.Message);
            }
        });
    }
}
