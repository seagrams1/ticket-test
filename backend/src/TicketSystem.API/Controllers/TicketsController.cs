using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.API.DTOs;
using TicketSystem.API.Models;
using TicketSystem.API.Services;

namespace TicketSystem.API.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public class TicketsController(ITicketService ticketService) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private UserRole CurrentUserRole =>
        Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role), out var role)
            ? role
            : UserRole.Submitter;

    // GET /api/tickets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketSummaryDto>>> GetAll()
    {
        var tickets = await ticketService.GetAllAsync(CurrentUserId, CurrentUserRole);
        return Ok(tickets);
    }

    // GET /api/tickets/stats
    [HttpGet("stats")]
    public async Task<ActionResult<TicketStatsDto>> GetStats()
    {
        var stats = await ticketService.GetStatsAsync(CurrentUserId, CurrentUserRole);
        return Ok(stats);
    }

    // GET /api/tickets/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailDto>> GetById(int id)
    {
        var ticket = await ticketService.GetByIdAsync(id);
        if (ticket is null) return NotFound();

        var allowed = CurrentUserRole switch
        {
            UserRole.Admin => true,
            UserRole.Agent => ticket.AssignedToId == CurrentUserId || ticket.AssignedToId is null,
            UserRole.Submitter => ticket.CreatedById == CurrentUserId,
            _ => false
        };

        if (!allowed) return Forbid();

        return Ok(ticket);
    }

    // POST /api/tickets
    [HttpPost]
    public async Task<ActionResult<TicketDetailDto>> Create([FromBody] CreateTicketRequest request)
    {
        var ticket = await ticketService.CreateAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    // PUT /api/tickets/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult<TicketDetailDto>> Update(int id, [FromBody] UpdateTicketRequest request)
    {
        // Submitters cannot update tickets
        if (CurrentUserRole == UserRole.Submitter)
            return Forbid();

        var existing = await ticketService.GetByIdAsync(id);
        if (existing is null) return NotFound();

        UpdateTicketRequest allowedRequest;

        if (CurrentUserRole == UserRole.Admin)
        {
            // Admin: can update Status on any ticket; can assign any unassigned ticket to any Agent
            if (request.AssignedToId.HasValue &&
                existing.AssignedToId is not null &&
                request.AssignedToId != existing.AssignedToId)
            {
                // Already assigned — cannot reassign via PUT (use /assign)
                return Forbid();
            }
            allowedRequest = request;
        }
        else // Agent
        {
            // Ticket must be assigned to current agent or unassigned
            if (existing.AssignedToId is not null && existing.AssignedToId != CurrentUserId)
                return Forbid();

            TicketStatus? allowedStatus = null;
            int? allowedAssignedToId = existing.AssignedToId; // keep as-is by default

            if (existing.AssignedToId == CurrentUserId)
            {
                // Can update Status only
                allowedStatus = request.Status;
            }
            else
            {
                // Ticket is unassigned — can self-assign
                if (request.AssignedToId.HasValue)
                {
                    if (request.AssignedToId != CurrentUserId)
                        return Forbid(); // Agents can only assign to themselves
                    allowedAssignedToId = CurrentUserId;
                }
                // Status update not allowed on unassigned tickets
            }

            allowedRequest = new UpdateTicketRequest(
                request.Title,       // allow title/description edits too
                request.Description,
                allowedStatus,
                allowedAssignedToId
            );
        }

        var ticket = await ticketService.UpdateAsync(id, allowedRequest, CurrentUserId);
        if (ticket is null) return NotFound();

        return Ok(ticket);
    }

    // POST /api/tickets/{id}/assign
    [HttpPost("{id:int}/assign")]
    public async Task<ActionResult<TicketDetailDto>> Assign(int id, [FromBody] AssignTicketRequest request)
    {
        if (CurrentUserRole == UserRole.Submitter)
            return Forbid();

        var existing = await ticketService.GetByIdAsync(id);
        if (existing is null) return NotFound();

        if (existing.AssignedToId is not null)
            return Conflict(new { message = "Ticket is already assigned. Unassign first." });

        int? targetId;

        if (CurrentUserRole == UserRole.Agent)
        {
            // Agents can only assign to themselves
            targetId = CurrentUserId;
        }
        else // Admin
        {
            // Admin can assign to any agent, defaults to themselves if not specified
            targetId = request.AssignedToId ?? CurrentUserId;
        }

        var ticket = await ticketService.AssignAsync(id, targetId, CurrentUserId);
        if (ticket is null) return NotFound();

        return Ok(ticket);
    }

    // POST /api/tickets/{id}/comments
    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<TicketCommentDto>> AddComment(int id, [FromBody] AddCommentRequest request)
    {
        // Verify the requesting user has access to this ticket
        var existing = await ticketService.GetByIdAsync(id);
        if (existing is null) return NotFound();

        var canAccess = CurrentUserRole switch
        {
            UserRole.Admin => true,
            UserRole.Agent => existing.AssignedToId == CurrentUserId || existing.AssignedToId is null,
            UserRole.Submitter => existing.CreatedById == CurrentUserId,
            _ => false
        };

        if (!canAccess) return Forbid();

        var comment = await ticketService.AddCommentAsync(id, request, CurrentUserId);
        if (comment is null) return NotFound();

        return CreatedAtAction(nameof(GetById), new { id }, comment);
    }
}
