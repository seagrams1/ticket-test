using System.ComponentModel.DataAnnotations;
using TicketSystem.API.Models;

namespace TicketSystem.API.DTOs;

public record CreateTicketRequest(
    [Required][MaxLength(255)] string Title,
    string Description = "",
    TicketPriority Priority = TicketPriority.Medium
);

public record UpdateTicketRequest(
    string? Title,
    string? Description,
    TicketStatus? Status,
    int? AssignedToId,
    TicketPriority? Priority = null
);

public record AssignTicketRequest(
    int? AssignedToId
);

public record AddCommentRequest(
    [Required] string Content
);

public record EditCommentRequest(
    [Required] string Content
);

// Query parameters for GET /api/tickets
public record TicketQueryParams(
    string? Search = null,
    TicketStatus? Status = null,
    bool AssignedToMe = false,
    int Page = 1,
    int PageSize = 20
);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record TicketSummaryDto(
    int Id,
    string Title,
    string Status,
    string Priority,
    string CreatedBy,
    int CreatedById,
    string? AssignedTo,
    int? AssignedToId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record TicketDetailDto(
    int Id,
    string Title,
    string Description,
    string Status,
    string Priority,
    int CreatedById,
    string CreatedBy,
    int? AssignedToId,
    string? AssignedTo,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IEnumerable<TicketCommentDto> Comments,
    IEnumerable<TicketHistoryDto> History
);

public record TicketCommentDto(
    int Id,
    int TicketId,
    int AuthorId,
    string Author,
    string Content,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record TicketHistoryDto(
    int Id,
    int TicketId,
    string FieldChanged,
    string? OldValue,
    string? NewValue,
    int ChangedById,
    string ChangedBy,
    DateTime CreatedAt
);

public record AgentDto(
    int Id,
    string Username
);

public record TicketStatsDto(
    int OpenCount,
    int InProgressCount,
    int ResolvedTodayCount,
    int TotalVisible
);
