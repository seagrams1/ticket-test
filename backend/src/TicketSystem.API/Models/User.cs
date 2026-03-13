using System.ComponentModel.DataAnnotations;

namespace TicketSystem.API.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Submitter;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Ticket> CreatedTickets { get; set; } = [];
    public ICollection<Ticket> AssignedTickets { get; set; } = [];
    public ICollection<TicketComment> Comments { get; set; } = [];
    public ICollection<TicketHistory> HistoryEntries { get; set; } = [];
}
