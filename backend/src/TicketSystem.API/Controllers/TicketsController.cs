using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.API.DTOs;
using TicketSystem.API.Models;
using TicketSystem.API.Services;

namespace TicketSystem.API.Controllers;

/// <summary>
/// Manages support tickets: CRUD, assignment, comments, history, and statistics.
/// All endpoints require authentication. Role-based access controls which tickets a user can see or modify.
/// </summary>
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

    /// <summary>
    /// Returns a paginated, filtered list of tickets visible to the current user.
    /// Admins see all tickets; agents see unassigned + assigned-to-them; submitters see only their own.
    /// </summary>
    /// <param name="search">Optional full-text search applied to title and description.</param>
    /// <param name="status">Optional status filter (Open, InProgress, Paused, Resolved, Unresolved).</param>
    /// <param name="assignedToMe">When true, returns only tickets assigned to the current user.</param>
    /// <param name="page">Page number (1-indexed). Defaults to 1.</param>
    /// <param name="pageSize">Items per page (1–100). Defaults to 20.</param>
    /// <returns>Paged result with ticket summaries and total count.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TicketSummaryDto>), StatusCodes.Status200OK)]
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

    /// <summary>
    /// Returns ticket statistics (open/in-progress/resolved-today counts) for the current user's visible scope.
    /// </summary>
    /// <returns>Counts by status for the dashboard.</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(TicketStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TicketStatsDto>> GetStats()
    {
        var stats = await ticketService.GetStatsAsync(CurrentUserId, CurrentUserRole);
        return Ok(stats);
    }

    /// <summary>
    /// Returns the full details of a single ticket including comments and history.
    /// Access is role-scoped: submitters can only view their own tickets; agents can view unassigned or assigned-to-them.
    /// </summary>
    /// <param name="id">The ticket ID.</param>
    /// <returns>Detailed ticket DTO.</returns>
    /// <response code="200">Ticket found and accessible.</response>
    /// <response code="403">Current user does not have access to this ticket.</response>
    /// <response code="404">Ticket not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Creates a new support ticket. Any authenticated user may submit a ticket.
    /// </summary>
    /// <param name="request">Ticket creation payload: title, optional description, and optional priority.</param>
    /// <returns>The newly created ticket with full details.</returns>
    /// <response code="201">Ticket created successfully.</response>
    [HttpPost]
    [ProducesResponseType(typeof(TicketDetailDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<TicketDetailDto>> Create([FromBody] CreateTicketRequest request)
    {
        var ticket = await ticketService.CreateAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    /// <summary>
    /// Updates an existing ticket's title, description, status, assigned agent, or priority.
    /// Submitters cannot update tickets. Agents can only update tickets assigned to them or self-assign.
    /// Admins have full update access with the constraint that they cannot reassign an already-assigned ticket to a different agent.
    /// </summary>
    /// <param name="id">The ticket ID.</param>
    /// <param name="request">Fields to update (all optional).</param>
    /// <returns>Updated ticket with full details.</returns>
    /// <response code="200">Ticket updated successfully.</response>
    /// <response code="403">Insufficient permissions to perform this update.</response>
    /// <response code="404">Ticket not found.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Assigns an unassigned ticket to an agent.
    /// Agents can only self-assign; admins can assign to any agent.
    /// A ticket must be unassigned before it can be assigned via this endpoint.
    /// </summary>
    /// <param name="id">The ticket ID.</param>
    /// <param name="request">Optional agent ID (admins only; agents always self-assign).</param>
    /// <returns>Updated ticket with assignment applied.</returns>
    /// <response code="200">Ticket assigned successfully.</response>
    /// <response code="403">Submitters cannot assign tickets.</response>
    /// <response code="404">Ticket not found.</response>
    /// <response code="409">Ticket is already assigned.</response>
    [HttpPost("{id:int}/assign")]
    [ProducesResponseType(typeof(TicketDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

    /// <summary>
    /// Adds a comment to a ticket. Access is role-scoped (same rules as viewing the ticket).
    /// </summary>
    /// <param name="id">The ticket ID.</param>
    /// <param name="request">Comment content.</param>
    /// <returns>The newly created comment.</returns>
    /// <response code="201">Comment added successfully.</response>
    /// <response code="403">User does not have access to comment on this ticket.</response>
    /// <response code="404">Ticket not found.</response>
    [HttpPost("{id:int}/comments")]
    [ProducesResponseType(typeof(TicketCommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Edits the content of an existing comment. Only the comment's author may edit it.
    /// </summary>
    /// <param name="id">The ticket ID.</param>
    /// <param name="commentId">The comment ID to edit.</param>
    /// <param name="request">New comment content.</param>
    /// <returns>The updated comment.</returns>
    /// <response code="200">Comment updated successfully.</response>
    /// <response code="403">User is not the comment author.</response>
    /// <response code="404">Ticket or comment not found.</response>
    [HttpPut("{id:int}/comments/{commentId:int}")]
    [ProducesResponseType(typeof(TicketCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    /// <summary>
    /// Deletes a comment from a ticket. Authors may delete their own comments; admins may delete any comment.
    /// </summary>
    /// <param name="id">The ticket ID.</param>
    /// <param name="commentId">The comment ID to delete.</param>
    /// <returns>204 No Content on success.</returns>
    /// <response code="204">Comment deleted successfully.</response>
    /// <response code="403">User does not have permission to delete this comment.</response>
    /// <response code="404">Ticket or comment not found.</response>
    [HttpDelete("{id:int}/comments/{commentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
