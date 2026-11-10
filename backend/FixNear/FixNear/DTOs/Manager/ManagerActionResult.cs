namespace FixNear.DTOs.Manager;

public record ManagerActionResult(
    int StatusCode,
    string Message,
    object? Data = null);
