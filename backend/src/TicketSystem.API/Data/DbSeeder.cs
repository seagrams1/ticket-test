using Microsoft.EntityFrameworkCore;
using TicketSystem.API.Models;

namespace TicketSystem.API.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Run pending migrations
        await context.Database.MigrateAsync();

        // Seed users if none exist
        if (await context.Users.AnyAsync())
            return;

        var users = new List<User>
        {
            new()
            {
                Username = "submitter",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
                Role = UserRole.Submitter,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Username = "agent",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
                Role = UserRole.Agent,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1!"),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        // Seed a sample ticket
        var submitter = users[0];
        var agent = users[1];

        var ticket = new Ticket
        {
            Title = "Sample support ticket",
            Description = "This is a seed ticket for testing purposes.",
            Status = TicketStatus.Open,
            CreatedById = submitter.Id,
            AssignedToId = agent.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();
    }
}
