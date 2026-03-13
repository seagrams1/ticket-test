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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TicketSummaryDto>>> GetAll()
    {
        var tickets = await ticketService.GetAllAsync(CurrentUserId, CurrentUserRole);
        return Ok(tickets);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TicketDetailDto>> GetById(int id)
    {
        var ticket = await ticketService.GetByIdAsync(id);
        if (ticket is null) return NotFound();

        // Submitters can only see their own tickets
        if (CurrentUserRole == UserRole.Submitter && ticket.CreatedById != CurrentUserId)
            return Forbid();

        return Ok(ticket);
    }

    [HttpPost]
    public async Task<ActionResult<TicketDetailDto>> Create([FromBody] CreateTicketRequest request)
    {
        var ticket = await ticketService.CreateAsync(request, CurrentUserId);
        return CreatedAtAction(nameof(GetById), new { id = ticket.Id }, ticket);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TicketDetailDto>> Update(int id, [FromBody] UpdateTicketRequest request)
    {
        // Only Agents and Admins can assign tickets; submitters can update their own
        var existing = await ticketService.GetByIdAsync(id);
        if (existing is null) return NotFound();

        if (CurrentUserRole == UserRole.Submitter && existing.CreatedById != CurrentUserId)
            return Forbid();

        var ticket = await ticketService.UpdateAsync(id, request, CurrentUserId);
        if (ticket is null) return NotFound();

        return Ok(ticket);
    }

    [HttpPost("{id:int}/comments")]
    public async Task<ActionResult<TicketCommentDto>> AddComment(int id, [FromBody] AddCommentRequest request)
    {
        var comment = await ticketService.AddCommentAsync(id, request, CurrentUserId);
        if (comment is null) return NotFound();

        return CreatedAtAction(nameof(GetById), new { id }, comment);
    }
}
