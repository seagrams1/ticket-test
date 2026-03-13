using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.API.Models;

public class TicketHistory
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int ChangedById { get; set; }

    [Required]
    [MaxLength(100)]
    public string FieldChanged { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(TicketId))]
    public Ticket Ticket { get; set; } = null!;

    [ForeignKey(nameof(ChangedById))]
    public User ChangedBy { get; set; } = null!;
}
