using System.ComponentModel.DataAnnotations;

namespace TicketSystem.API.DTOs;

public record RegisterRequest(
    [Required][MaxLength(100)] string Username,
    [Required][MinLength(6)] string Password,
    string? Role = null
);

public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

public record AuthResponse(
    string Token,
    string Username,
    string Role,
    int UserId
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required][MinLength(6)] string NewPassword
);
