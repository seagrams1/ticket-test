using Microsoft.EntityFrameworkCore;
using TicketSystem.API.Data;
using TicketSystem.API.DTOs;
using TicketSystem.API.Models;
using TicketSystem.API.Services;

namespace TicketSystem.Tests.Services;

public class TicketServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TicketService _service;

    public TicketServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new TicketService(_context);

        SeedTestData();
    }

    private void SeedTestData()
    {
        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = "hash",
            Role = UserRole.Agent,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnTicketWithCorrectDetails()
    {
        // Arrange
        var request = new CreateTicketRequest("Test Ticket", "Test description");

        // Act
        var result = await _service.CreateAsync(request, createdById: 1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Ticket", result.Title);
        Assert.Equal("Test description", result.Description);
        Assert.Equal("Open", result.Status);
        Assert.Equal(1, result.CreatedById);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllTicketsForAgent()
    {
        // Arrange
        await _service.CreateAsync(new CreateTicketRequest("Ticket 1"), 1);
        await _service.CreateAsync(new CreateTicketRequest("Ticket 2"), 1);

        // Act
        var results = await _service.GetAllAsync(requestingUserId: 1, requestingUserRole: UserRole.Agent);

        // Assert
        Assert.Equal(2, results.Count());
    }

    [Fact]
    public async Task GetAllAsync_SubmitterShouldOnlySeeOwnTickets()
    {
        // Arrange
        var submitter = new User
        {
            Id = 2,
            Username = "submitter",
            PasswordHash = "hash",
            Role = UserRole.Submitter,
            CreatedAt = DateTime.UtcNow
        };
        _context.Users.Add(submitter);
        await _context.SaveChangesAsync();

        await _service.CreateAsync(new CreateTicketRequest("Agent Ticket"), createdById: 1);
        await _service.CreateAsync(new CreateTicketRequest("Submitter Ticket"), createdById: 2);

        // Act
        var results = await _service.GetAllAsync(requestingUserId: 2, requestingUserRole: UserRole.Submitter);

        // Assert
        Assert.Single(results);
        Assert.Equal("Submitter Ticket", results.First().Title);
    }

    [Fact]
    public async Task UpdateAsync_ShouldCreateHistoryEntry()
    {
        // Arrange
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Original Title"), 1);
        var updateRequest = new UpdateTicketRequest(Title: "New Title", null, null, null);

        // Act
        var updated = await _service.UpdateAsync(ticket.Id, updateRequest, changedById: 1);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal("New Title", updated.Title);
        Assert.Single(updated.History);
        Assert.Equal("Title", updated.History.First().FieldChanged);
        Assert.Equal("Original Title", updated.History.First().OldValue);
        Assert.Equal("New Title", updated.History.First().NewValue);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForNonExistentTicket()
    {
        var result = await _service.GetByIdAsync(9999);
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
