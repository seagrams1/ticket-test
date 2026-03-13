using Microsoft.EntityFrameworkCore;
using TicketSystem.API.Data;
using TicketSystem.API.DTOs;
using TicketSystem.API.Models;

namespace TicketSystem.API.Services;

public interface ITicketService
{
    Task<PagedResult<TicketSummaryDto>> GetAllAsync(TicketQueryParams queryParams, int? requestingUserId = null, UserRole? requestingUserRole = null);
    Task<TicketDetailDto?> GetByIdAsync(int id);
    Task<TicketDetailDto> CreateAsync(CreateTicketRequest request, int createdById);
    Task<TicketDetailDto?> UpdateAsync(int id, UpdateTicketRequest request, int changedById);
    Task<TicketCommentDto?> AddCommentAsync(int ticketId, AddCommentRequest request, int authorId);
    Task<TicketCommentDto?> EditCommentAsync(int ticketId, int commentId, EditCommentRequest request, int requestingUserId, UserRole requestingUserRole);
    Task<bool?> DeleteCommentAsync(int ticketId, int commentId, int requestingUserId, UserRole requestingUserRole);
    Task<TicketDetailDto?> AssignAsync(int ticketId, int? assignedToId, int changedById);
    Task<TicketStatsDto> GetStatsAsync(int requestingUserId, UserRole requestingUserRole);
}

public class TicketService(AppDbContext context) : ITicketService
{
    public async Task<PagedResult<TicketSummaryDto>> GetAllAsync(
        TicketQueryParams queryParams,
        int? requestingUserId = null,
        UserRole? requestingUserRole = null)
    {
        var query = BuildTicketQuery(requestingUserId, requestingUserRole);

        // Apply search filter (title + description)
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(search) ||
                t.Description.ToLower().Contains(search));
        }

        // Apply status filter
        if (queryParams.Status.HasValue)
        {
            query = query.Where(t => t.Status == queryParams.Status.Value);
        }

        // Apply assignedToMe filter (agents/admins only)
        if (queryParams.AssignedToMe && requestingUserId.HasValue)
        {
            query = query.Where(t => t.AssignedToId == requestingUserId.Value);
        }

        var totalCount = await query.CountAsync();

        var page = Math.Max(1, queryParams.Page);
        var pageSize = Math.Clamp(queryParams.PageSize, 1, 100);

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = tickets.Select(t => new TicketSummaryDto(
            t.Id,
            t.Title,
            t.Status.ToString(),
            t.Priority.ToString(),
            t.CreatedBy.Username,
            t.CreatedById,
            t.AssignedTo?.Username,
            t.AssignedToId,
            t.CreatedAt,
            t.UpdatedAt
        ));

        return new PagedResult<TicketSummaryDto>(items, totalCount, page, pageSize);
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
            Priority = request.Priority,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();

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

        if (request.Priority.HasValue && request.Priority.Value != ticket.Priority)
        {
            historyEntries.Add(CreateHistoryEntry(ticket.Id, changedById, "Priority", ticket.Priority.ToString(), request.Priority.Value.ToString()));
            ticket.Priority = request.Priority.Value;
        }

        if (historyEntries.Count > 0)
        {
            ticket.UpdatedAt = DateTime.UtcNow;
            context.TicketHistories.AddRange(historyEntries);
        }

        await context.SaveChangesAsync();

        return (await GetByIdAsync(ticket.Id))!;
    }

    public async Task<TicketDetailDto?> AssignAsync(int ticketId, int? assignedToId, int changedById)
    {
        var ticket = await context.Tickets.FindAsync(ticketId);
        if (ticket is null) return null;

        var oldValue = ticket.AssignedToId?.ToString();
        var newValue = assignedToId?.ToString();

        ticket.AssignedToId = assignedToId;
        ticket.UpdatedAt = DateTime.UtcNow;

        context.TicketHistories.Add(CreateHistoryEntry(ticketId, changedById, "AssignedToId", oldValue, newValue));
        await context.SaveChangesAsync();

        return (await GetByIdAsync(ticketId))!;
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

        // History entry
        context.TicketHistories.Add(CreateHistoryEntry(ticketId, authorId, "Comment", null, "added"));
        ticket.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        var author = await context.Users.FindAsync(authorId);

        return new TicketCommentDto(
            comment.Id,
            comment.TicketId,
            comment.AuthorId,
            author?.Username ?? "Unknown",
            comment.Content,
            comment.CreatedAt,
            comment.UpdatedAt
        );
    }

    public async Task<TicketCommentDto?> EditCommentAsync(
        int ticketId,
        int commentId,
        EditCommentRequest request,
        int requestingUserId,
        UserRole requestingUserRole)
    {
        var comment = await context.TicketComments
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TicketId == ticketId);

        if (comment is null) return null;

        // Only the author can edit
        if (comment.AuthorId != requestingUserId)
            return null; // caller maps to Forbid

        var oldContent = comment.Content;
        comment.Content = request.Content;
        comment.UpdatedAt = DateTime.UtcNow;

        // Update ticket's UpdatedAt
        var ticket = await context.Tickets.FindAsync(ticketId);
        if (ticket is not null) ticket.UpdatedAt = DateTime.UtcNow;

        context.TicketHistories.Add(CreateHistoryEntry(ticketId, requestingUserId, "Comment", oldContent, "edited"));

        await context.SaveChangesAsync();

        return new TicketCommentDto(
            comment.Id,
            comment.TicketId,
            comment.AuthorId,
            comment.Author.Username,
            comment.Content,
            comment.CreatedAt,
            comment.UpdatedAt
        );
    }

    public async Task<bool?> DeleteCommentAsync(
        int ticketId,
        int commentId,
        int requestingUserId,
        UserRole requestingUserRole)
    {
        var comment = await context.TicketComments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TicketId == ticketId);

        if (comment is null) return null;

        // Only author or Admin can delete
        var isAuthor = comment.AuthorId == requestingUserId;
        var isAdmin = requestingUserRole == UserRole.Admin;

        if (!isAuthor && !isAdmin)
            return false; // caller maps to Forbid

        context.TicketComments.Remove(comment);

        // Update ticket's UpdatedAt
        var ticket = await context.Tickets.FindAsync(ticketId);
        if (ticket is not null) ticket.UpdatedAt = DateTime.UtcNow;

        context.TicketHistories.Add(CreateHistoryEntry(ticketId, requestingUserId, "Comment", comment.Content, "deleted"));

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<TicketStatsDto> GetStatsAsync(int requestingUserId, UserRole requestingUserRole)
    {
        var query = BuildTicketQuery(requestingUserId, requestingUserRole);
        var tickets = await query.ToListAsync();
        var today = DateTime.UtcNow.Date;

        return new TicketStatsDto(
            tickets.Count(t => t.Status == TicketStatus.Open),
            tickets.Count(t => t.Status == TicketStatus.InProgress),
            tickets.Count(t => t.Status == TicketStatus.Resolved && t.UpdatedAt.Date == today),
            tickets.Count
        );
    }

    // ─── Private helpers ────────────────────────────────────────────────────────

    private IQueryable<Ticket> BuildTicketQuery(int? requestingUserId, UserRole? requestingUserRole)
    {
        var query = context.Tickets
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .AsQueryable();

        if (requestingUserRole == UserRole.Submitter && requestingUserId.HasValue)
        {
            query = query.Where(t => t.CreatedById == requestingUserId.Value);
        }
        else if (requestingUserRole == UserRole.Agent && requestingUserId.HasValue)
        {
            query = query.Where(t => t.AssignedToId == requestingUserId.Value || t.AssignedToId == null);
        }
        // Admins see all tickets

        return query;
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
            ticket.Priority.ToString(),
            ticket.CreatedById,
            ticket.CreatedBy.Username,
            ticket.AssignedToId,
            ticket.AssignedTo?.Username,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.Comments.OrderBy(c => c.CreatedAt).Select(c => new TicketCommentDto(
                c.Id, c.TicketId, c.AuthorId, c.Author.Username, c.Content, c.CreatedAt, c.UpdatedAt
            )),
            ticket.History.OrderBy(h => h.CreatedAt).Select(h => new TicketHistoryDto(
                h.Id, h.TicketId, h.FieldChanged, h.OldValue, h.NewValue, h.ChangedById, h.ChangedBy.Username, h.CreatedAt
            ))
        );
}
