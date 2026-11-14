namespace FixNear.Features.Manager.DTOs;

public record ManagerActionResult(
    int StatusCode,
    string Message,
    object? Data = null);
