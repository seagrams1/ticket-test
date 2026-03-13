using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.API.Models;

public class TicketComment
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public int AuthorId { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(TicketId))]
    public Ticket Ticket { get; set; } = null!;

    [ForeignKey(nameof(AuthorId))]
    public User Author { get; set; } = null!;
}
