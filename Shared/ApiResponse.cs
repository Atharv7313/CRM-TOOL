using System.Text.Json.Serialization;

namespace Shared.CL;

public class ApiResponse<T>
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; init; }

    public static ApiResponse<T> Ok(T data, string message = "Success")
        => new() { IsSuccess = true, Message = message, Data = data };

    public static ApiResponse<T> OkOnly(string message = "Success")
        => new() { IsSuccess = true, Message = message };

    public static ApiResponse<T> Success(T data, string message = "Success")
        => new() { IsSuccess = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null)
        => new() { IsSuccess = false, Message = message, Errors = errors };
}
