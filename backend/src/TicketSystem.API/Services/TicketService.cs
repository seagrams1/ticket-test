using Microsoft.EntityFrameworkCore;
using TicketSystem.API.Data;
using TicketSystem.API.DTOs;
using TicketSystem.API.Models;

namespace TicketSystem.API.Services;

public interface ITicketService
{
    Task<IEnumerable<TicketSummaryDto>> GetAllAsync(int? requestingUserId = null, UserRole? requestingUserRole = null);
    Task<TicketDetailDto?> GetByIdAsync(int id);
    Task<TicketDetailDto> CreateAsync(CreateTicketRequest request, int createdById);
    Task<TicketDetailDto?> UpdateAsync(int id, UpdateTicketRequest request, int changedById);
    Task<TicketCommentDto?> AddCommentAsync(int ticketId, AddCommentRequest request, int authorId);
}

public class TicketService(AppDbContext context) : ITicketService
{
    public async Task<IEnumerable<TicketSummaryDto>> GetAllAsync(int? requestingUserId = null, UserRole? requestingUserRole = null)
    {
        var query = context.Tickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .AsQueryable();

        // Submitters can only see their own tickets
        if (requestingUserRole == UserRole.Submitter && requestingUserId.HasValue)
        {
            query = query.Where(t => t.CreatedById == requestingUserId.Value);
        }

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return tickets.Select(t => new TicketSummaryDto(
            t.Id,
            t.Title,
            t.Status.ToString(),
            t.CreatedBy.Username,
            t.AssignedTo?.Username,
            t.CreatedAt,
            t.UpdatedAt
        ));
    }

    public async Task<TicketDetailDto?> GetByIdAsync(int id)
    {
        var ticket = await context.Tickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .Include(t => t.History).ThenInclude(h => h.ChangedBy)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null) return null;

        return MapToDetailDto(ticket);
    }

    public async Task<TicketDetailDto> CreateAsync(CreateTicketRequest request, int createdById)
    {
        var ticket = new Ticket
        {
            Title = request.Title,
            Description = request.Description,
            Status = TicketStatus.Open,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

        // Reload with navigation properties
        return (await GetByIdAsync(ticket.Id))!;
    }

    public async Task<TicketDetailDto?> UpdateAsync(int id, UpdateTicketRequest request, int changedById)
    {
        var ticket = await context.Tickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .Include(t => t.History).ThenInclude(h => h.ChangedBy)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket is null) return null;

        var historyEntries = new List<TicketHistory>();

        if (request.Title is not null && request.Title != ticket.Title)
        {
            historyEntries.Add(CreateHistoryEntry(ticket.Id, changedById, "Title", ticket.Title, request.Title));
            ticket.Title = request.Title;
        }

        if (request.Description is not null && request.Description != ticket.Description)
        {
            historyEntries.Add(CreateHistoryEntry(ticket.Id, changedById, "Description", ticket.Description, request.Description));
            ticket.Description = request.Description;
        }

        if (request.Status.HasValue && request.Status.Value != ticket.Status)
        {
            historyEntries.Add(CreateHistoryEntry(ticket.Id, changedById, "Status", ticket.Status.ToString(), request.Status.Value.ToString()));
            ticket.Status = request.Status.Value;
        }

        if (request.AssignedToId != ticket.AssignedToId)
        {
            var oldValue = ticket.AssignedToId?.ToString();
            var newValue = request.AssignedToId?.ToString();
            historyEntries.Add(CreateHistoryEntry(ticket.Id, changedById, "AssignedToId", oldValue, newValue));
            ticket.AssignedToId = request.AssignedToId;
        }

        if (historyEntries.Count > 0)
        {
            ticket.UpdatedAt = DateTime.UtcNow;
            context.TicketHistories.AddRange(historyEntries);
        }

        await context.SaveChangesAsync();

        return (await GetByIdAsync(ticket.Id))!;
    }

    public async Task<TicketCommentDto?> AddCommentAsync(int ticketId, AddCommentRequest request, int authorId)
    {
        var ticket = await context.Tickets.FindAsync(ticketId);
        if (ticket is null) return null;

        var comment = new TicketComment
        {
            TicketId = ticketId,
            AuthorId = authorId,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow
        };

        context.TicketComments.Add(comment);
        await context.SaveChangesAsync();

        var author = await context.Users.FindAsync(authorId);

        return new TicketCommentDto(
            comment.Id,
            comment.TicketId,
            comment.AuthorId,
            author?.Username ?? "Unknown",
            comment.Content,
            comment.CreatedAt
        );
    }

    private static TicketHistory CreateHistoryEntry(int ticketId, int changedById, string field, string? oldValue, string? newValue) =>
        new()
        {
            TicketId = ticketId,
            ChangedById = changedById,
            FieldChanged = field,
            OldValue = oldValue,
            NewValue = newValue,
            CreatedAt = DateTime.UtcNow
        };

    private static TicketDetailDto MapToDetailDto(Ticket ticket) =>
        new(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.Status.ToString(),
            ticket.CreatedById,
            ticket.CreatedBy.Username,
            ticket.AssignedToId,
            ticket.AssignedTo?.Username,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.Comments.OrderBy(c => c.CreatedAt).Select(c => new TicketCommentDto(
                c.Id, c.TicketId, c.AuthorId, c.Author.Username, c.Content, c.CreatedAt
            )),
            ticket.History.OrderBy(h => h.CreatedAt).Select(h => new TicketHistoryDto(
                h.Id, h.TicketId, h.FieldChanged, h.OldValue, h.NewValue, h.ChangedById, h.ChangedBy.Username, h.CreatedAt
            ))
        );
}
