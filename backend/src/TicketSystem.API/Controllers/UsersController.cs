using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.API.Data;
using TicketSystem.API.DTOs;
using TicketSystem.API.Models;

namespace TicketSystem.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(AppDbContext context) : ControllerBase
{
    // GET /api/users/agents — Admin only
    [HttpGet("agents")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<AgentDto>>> GetAgents()
    {
        var agents = await context.Users
            .Where(u => u.Role == UserRole.Agent)
            .OrderBy(u => u.Username)
            .Select(u => new AgentDto(u.Id, u.Username))
            .ToListAsync();

        return Ok(agents);
    }
}
