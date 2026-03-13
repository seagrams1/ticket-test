using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.API.Models;

public class Ticket
{
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public int CreatedById { get; set; }

    public int? AssignedToId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(CreatedById))]
    public User CreatedBy { get; set; } = null!;

    [ForeignKey(nameof(AssignedToId))]
    public User? AssignedTo { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = [];
    public ICollection<TicketHistory> History { get; set; } = [];
}
