using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.API.Data;
using TicketSystem.API.DTOs;
using TicketSystem.API.Models;
using TicketSystem.API.Services;

namespace TicketSystem.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext context, IJwtService jwtService) : ControllerBase
{
    [HttpPost("register")]
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

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials." });

        var token = jwtService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Username, user.Role.ToString(), user.Id));
    }
}
