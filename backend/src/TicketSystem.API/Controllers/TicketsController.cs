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
    public async Task<ActionResult<PagedResult<TicketSummaryDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] bool assignedToMe = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        TicketStatus? statusEnum = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<TicketStatus>(status, true, out var parsedStatus))
        {
            statusEnum = parsedStatus;
        }

        var queryParams = new TicketQueryParams(search, statusEnum, assignedToMe, page, pageSize);
        var result = await ticketService.GetAllAsync(queryParams, CurrentUserId, CurrentUserRole);
        return Ok(result);
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
        if (CurrentUserRole == UserRole.Submitter)
            return Forbid();

        var existing = await ticketService.GetByIdAsync(id);
        if (existing is null) return NotFound();

        UpdateTicketRequest allowedRequest;

        if (CurrentUserRole == UserRole.Admin)
        {
            if (request.AssignedToId.HasValue &&
                existing.AssignedToId is not null &&
                request.AssignedToId != existing.AssignedToId)
            {
                return Forbid();
            }
            allowedRequest = request;
        }
        else // Agent
        {
            if (existing.AssignedToId is not null && existing.AssignedToId != CurrentUserId)
                return Forbid();

            TicketStatus? allowedStatus = null;
            int? allowedAssignedToId = existing.AssignedToId;

            if (existing.AssignedToId == CurrentUserId)
            {
                allowedStatus = request.Status;
            }
            else
            {
                if (request.AssignedToId.HasValue)
                {
                    if (request.AssignedToId != CurrentUserId)
                        return Forbid();
                    allowedAssignedToId = CurrentUserId;
                }
            }

            allowedRequest = new UpdateTicketRequest(
                request.Title,
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
            targetId = CurrentUserId;
        }
        else // Admin
        {
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

    // PUT /api/tickets/{id}/comments/{commentId}
    [HttpPut("{id:int}/comments/{commentId:int}")]
    public async Task<ActionResult<TicketCommentDto>> EditComment(
        int id,
        int commentId,
        [FromBody] EditCommentRequest request)
    {
        var existing = await ticketService.GetByIdAsync(id);
        if (existing is null) return NotFound();

        var result = await ticketService.EditCommentAsync(id, commentId, request, CurrentUserId, CurrentUserRole);

        if (result is null)
        {
            // Could be not found or forbidden — distinguish by checking if comment exists
            var commentExists = existing.Comments.Any(c => c.Id == commentId);
            if (!commentExists) return NotFound();
            return Forbid();
        }

        return Ok(result);
    }

    // DELETE /api/tickets/{id}/comments/{commentId}
    [HttpDelete("{id:int}/comments/{commentId:int}")]
    public async Task<IActionResult> DeleteComment(int id, int commentId)
    {
        var existing = await ticketService.GetByIdAsync(id);
        if (existing is null) return NotFound();

        var result = await ticketService.DeleteCommentAsync(id, commentId, CurrentUserId, CurrentUserRole);

        if (result is null)
        {
            var commentExists = existing.Comments.Any(c => c.Id == commentId);
            if (!commentExists) return NotFound();
            return Forbid();
        }

        if (!result.Value) return Forbid();

        return NoContent();
    }
}
