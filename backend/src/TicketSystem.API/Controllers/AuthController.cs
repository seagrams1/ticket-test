using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.API.Data;
using TicketSystem.API.DTOs;
using TicketSystem.API.Models;
using TicketSystem.API.Services;

namespace TicketSystem.API.Controllers;

/// <summary>
/// Handles user authentication: registration, login, and password management.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext context, IJwtService jwtService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Registers a new user account and returns a JWT token.
    /// </summary>
    /// <param name="request">Registration details: username, password, and optional role.</param>
    /// <returns>JWT auth response with token, username, role, and user ID.</returns>
    /// <response code="201">User registered successfully.</response>
    /// <response code="409">Username already taken.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (await context.Users.AnyAsync(u => u.Username == request.Username))
            return Conflict(new { message = "Username already taken." });

        var role = UserRole.Submitter;
        if (request.Role is not null && Enum.TryParse<UserRole>(request.Role, true, out var parsedRole))
            role = parsedRole;

        var user = new User
        {
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = jwtService.GenerateToken(user);
        return CreatedAtAction(nameof(Register), new AuthResponse(token, user.Username, user.Role.ToString(), user.Id));
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="request">Login credentials: username and password.</param>
    /// <returns>JWT auth response with token, username, role, and user ID.</returns>
    /// <response code="200">Login successful.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials." });

        var token = jwtService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Username, user.Role.ToString(), user.Id));
    }

    /// <summary>
    /// Changes the authenticated user's password.
    /// </summary>
    /// <param name="request">Current and new password.</param>
    /// <returns>Success message or error details.</returns>
    /// <response code="200">Password changed successfully.</response>
    /// <response code="400">Current password is incorrect.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = await context.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound(new { message = "User not found." });

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Current password is incorrect." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await context.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully." });
    }
}
