using Microsoft.EntityFrameworkCore;
using TicketSystem.API.Models;

namespace TicketSystem.API.Data;

/// <summary>
/// Seeds the database with realistic sample data for development and demo environments.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Run pending migrations
        await context.Database.MigrateAsync();

        // Seed users if none exist
        if (await context.Users.AnyAsync())
            return;

        var now = DateTime.UtcNow;

        // ─── Users ────────────────────────────────────────────────────────────────
        var submitter = new User
        {
            Username = "submitter",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
            Role = UserRole.Submitter,
            CreatedAt = now.AddDays(-30)
        };
        var agent = new User
        {
            Username = "agent",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
            Role = UserRole.Agent,
            CreatedAt = now.AddDays(-30)
        };
        var agent2 = new User
        {
            Username = "agent2",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
            Role = UserRole.Agent,
            CreatedAt = now.AddDays(-25)
        };
        var admin = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
            Role = UserRole.Admin,
            CreatedAt = now.AddDays(-30)
        };

        context.Users.AddRange(submitter, agent, agent2, admin);
        await context.SaveChangesAsync();

        // ─── Tickets ──────────────────────────────────────────────────────────────
        var ticket1 = new Ticket
        {
            Title = "Login page throws 500 error on bad credentials",
            Description = "When a user enters the wrong password, the login page shows a generic server error instead of a validation message. Steps: go to /login, enter username 'test', password 'wrong', click Sign In.",
            Status = TicketStatus.Open,
            Priority = TicketPriority.High,
            CreatedById = submitter.Id,
            CreatedAt = now.AddDays(-10),
            UpdatedAt = now.AddDays(-10)
        };

        var ticket2 = new Ticket
        {
            Title = "Dashboard statistics not updating in real time",
            Description = "The open/in-progress/resolved counters on the dashboard are stale until page refresh. Expected: counters should reflect current state after ticket changes.",
            Status = TicketStatus.InProgress,
            Priority = TicketPriority.Medium,
            CreatedById = submitter.Id,
            AssignedToId = agent.Id,
            CreatedAt = now.AddDays(-8),
            UpdatedAt = now.AddDays(-5)
        };

        var ticket3 = new Ticket
        {
            Title = "CSV export is missing the Priority column",
            Description = "Users who export ticket lists to CSV notice the Priority field is absent. All other columns are present. Tested on Chrome and Firefox.",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Low,
            CreatedById = submitter.Id,
            CreatedAt = now.AddDays(-6),
            UpdatedAt = now.AddDays(-6)
        };

        var ticket4 = new Ticket
        {
            Title = "Password reset emails are not being delivered",
            Description = "Multiple users report they never receive the password reset email. The admin panel shows the emails are queued but not sent. SMTP config may need updating.",
            Status = TicketStatus.InProgress,
            Priority = TicketPriority.Critical,
            CreatedById = submitter.Id,
            AssignedToId = agent2.Id,
            CreatedAt = now.AddDays(-5),
            UpdatedAt = now.AddDays(-2)
        };

        var ticket5 = new Ticket
        {
            Title = "Mobile layout breaks on screens narrower than 375px",
            Description = "On iPhone SE (375px wide) the ticket list table overflows horizontally. The action buttons are cut off. Needs responsive fix.",
            Status = TicketStatus.Resolved,
            Priority = TicketPriority.Medium,
            CreatedById = submitter.Id,
            AssignedToId = agent.Id,
            CreatedAt = now.AddDays(-14),
            UpdatedAt = now.AddDays(-1)
        };

        var ticket6 = new Ticket
        {
            Title = "Add dark mode support to the admin panel",
            Description = "Multiple team members have requested a dark mode toggle. Should respect the system preference (prefers-color-scheme) as default and allow manual override.",
            Status = TicketStatus.Open,
            Priority = TicketPriority.Low,
            CreatedById = submitter.Id,
            CreatedAt = now.AddDays(-3),
            UpdatedAt = now.AddDays(-3)
        };

        var ticket7 = new Ticket
        {
            Title = "API rate limiting needed for public endpoints",
            Description = "The /api/auth/login endpoint has no rate limiting, making it vulnerable to brute-force attacks. Recommend adding 10 req/min per IP limit.",
            Status = TicketStatus.Paused,
            Priority = TicketPriority.High,
            CreatedById = submitter.Id,
            AssignedToId = agent2.Id,
            CreatedAt = now.AddDays(-12),
            UpdatedAt = now.AddDays(-7)
        };

        var ticket8 = new Ticket
        {
            Title = "Ticket search is case-sensitive — should be case-insensitive",
            Description = "Searching for 'Login' returns no results but searching for 'login' works. All text search should be case-insensitive by default.",
            Status = TicketStatus.Resolved,
            Priority = TicketPriority.Medium,
            CreatedById = submitter.Id,
            AssignedToId = agent.Id,
            CreatedAt = now.AddDays(-20),
            UpdatedAt = now.AddDays(-3)
        };

        context.Tickets.AddRange(ticket1, ticket2, ticket3, ticket4, ticket5, ticket6, ticket7, ticket8);
        await context.SaveChangesAsync();

        // ─── Comments ─────────────────────────────────────────────────────────────
        var comment1 = new TicketComment
        {
            TicketId = ticket1.Id,
            AuthorId = agent.Id,
            Content = "Reproduced the issue. The error is a NullReferenceException in the auth middleware when the username doesn't exist in the database. Fix incoming.",
            CreatedAt = now.AddDays(-9)
        };

        var comment2 = new TicketComment
        {
            TicketId = ticket1.Id,
            AuthorId = submitter.Id,
            Content = "Thanks for the quick response! Let me know when there's a fix I can test.",
            CreatedAt = now.AddDays(-9).AddHours(2)
        };

        var comment3 = new TicketComment
        {
            TicketId = ticket2.Id,
            AuthorId = agent.Id,
            Content = "Investigating. The stats endpoint is cached with a 60-second TTL. I'll remove the cache or add a WebSocket push for real-time updates.",
            CreatedAt = now.AddDays(-6)
        };

        var comment4 = new TicketComment
        {
            TicketId = ticket4.Id,
            AuthorId = agent2.Id,
            Content = "Found the issue — the SMTP credentials in production config expired. Rotating now. Should be resolved within the hour.",
            CreatedAt = now.AddDays(-3)
        };

        var comment5 = new TicketComment
        {
            TicketId = ticket5.Id,
            AuthorId = agent.Id,
            Content = "Fixed! Added a responsive wrapper and switched the action buttons to a vertical stack below 400px. Deployed to staging.",
            CreatedAt = now.AddDays(-2)
        };

        var comment6 = new TicketComment
        {
            TicketId = ticket5.Id,
            AuthorId = submitter.Id,
            Content = "Confirmed fixed on my iPhone SE. Looks great, thank you!",
            CreatedAt = now.AddDays(-1)
        };

        context.TicketComments.AddRange(comment1, comment2, comment3, comment4, comment5, comment6);
        await context.SaveChangesAsync();

        // ─── History entries ───────────────────────────────────────────────────────
        context.TicketHistories.AddRange(
            // ticket2: assigned to agent, then status changed to InProgress
            new TicketHistory { TicketId = ticket2.Id, ChangedById = admin.Id, FieldChanged = "AssignedToId", OldValue = null, NewValue = agent.Id.ToString(), CreatedAt = now.AddDays(-8) },
            new TicketHistory { TicketId = ticket2.Id, ChangedById = agent.Id, FieldChanged = "Status", OldValue = "Open", NewValue = "InProgress", CreatedAt = now.AddDays(-7) },
            new TicketHistory { TicketId = ticket2.Id, ChangedById = agent.Id, FieldChanged = "Comment", OldValue = null, NewValue = "added", CreatedAt = now.AddDays(-6) },

            // ticket4: priority escalated to Critical, assigned to agent2
            new TicketHistory { TicketId = ticket4.Id, ChangedById = admin.Id, FieldChanged = "Priority", OldValue = "High", NewValue = "Critical", CreatedAt = now.AddDays(-4) },
            new TicketHistory { TicketId = ticket4.Id, ChangedById = admin.Id, FieldChanged = "AssignedToId", OldValue = null, NewValue = agent2.Id.ToString(), CreatedAt = now.AddDays(-4) },
            new TicketHistory { TicketId = ticket4.Id, ChangedById = agent2.Id, FieldChanged = "Status", OldValue = "Open", NewValue = "InProgress", CreatedAt = now.AddDays(-3) },
            new TicketHistory { TicketId = ticket4.Id, ChangedById = agent2.Id, FieldChanged = "Comment", OldValue = null, NewValue = "added", CreatedAt = now.AddDays(-3) },

            // ticket5: assigned, status progressed, resolved
            new TicketHistory { TicketId = ticket5.Id, ChangedById = admin.Id, FieldChanged = "AssignedToId", OldValue = null, NewValue = agent.Id.ToString(), CreatedAt = now.AddDays(-13) },
            new TicketHistory { TicketId = ticket5.Id, ChangedById = agent.Id, FieldChanged = "Status", OldValue = "Open", NewValue = "InProgress", CreatedAt = now.AddDays(-10) },
            new TicketHistory { TicketId = ticket5.Id, ChangedById = agent.Id, FieldChanged = "Comment", OldValue = null, NewValue = "added", CreatedAt = now.AddDays(-2) },
            new TicketHistory { TicketId = ticket5.Id, ChangedById = agent.Id, FieldChanged = "Status", OldValue = "InProgress", NewValue = "Resolved", CreatedAt = now.AddDays(-1) },

            // ticket7: assigned to agent2, then paused
            new TicketHistory { TicketId = ticket7.Id, ChangedById = admin.Id, FieldChanged = "AssignedToId", OldValue = null, NewValue = agent2.Id.ToString(), CreatedAt = now.AddDays(-11) },
            new TicketHistory { TicketId = ticket7.Id, ChangedById = agent2.Id, FieldChanged = "Status", OldValue = "Open", NewValue = "InProgress", CreatedAt = now.AddDays(-10) },
            new TicketHistory { TicketId = ticket7.Id, ChangedById = agent2.Id, FieldChanged = "Status", OldValue = "InProgress", NewValue = "Paused", CreatedAt = now.AddDays(-7) },

            // ticket8: assigned, progressed, resolved
            new TicketHistory { TicketId = ticket8.Id, ChangedById = admin.Id, FieldChanged = "AssignedToId", OldValue = null, NewValue = agent.Id.ToString(), CreatedAt = now.AddDays(-19) },
            new TicketHistory { TicketId = ticket8.Id, ChangedById = agent.Id, FieldChanged = "Status", OldValue = "Open", NewValue = "InProgress", CreatedAt = now.AddDays(-15) },
            new TicketHistory { TicketId = ticket8.Id, ChangedById = agent.Id, FieldChanged = "Status", OldValue = "InProgress", NewValue = "Resolved", CreatedAt = now.AddDays(-3) }
        );

        await context.SaveChangesAsync();
    }
}
