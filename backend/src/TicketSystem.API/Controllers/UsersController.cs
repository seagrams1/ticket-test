using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.API.Data;
using TicketSystem.API.DTOs;
using TicketSystem.API.Models;

namespace TicketSystem.API.Controllers;

/// <summary>
/// Provides user management operations (currently agent listing for assignment workflows).
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(AppDbContext context) : ControllerBase
{
    /// <summary>
    /// Returns a list of all users with the Agent role.
    /// Used in the admin UI to populate the ticket assignment dropdown.
    /// </summary>
    /// <returns>List of agents (ID and username).</returns>
    /// <response code="200">List of agents returned.</response>
    /// <response code="403">Caller does not have the Admin role.</response>
    [HttpGet("agents")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(IEnumerable<AgentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
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
