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

    // Shared user IDs
    private const int AdminId = 1;
    private const int AgentId = 2;
    private const int Agent2Id = 3;
    private const int SubmitterId = 4;

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
        _context.Users.AddRange(
            new User { Id = AdminId,    Username = "admin",     PasswordHash = "hash", Role = UserRole.Admin,     CreatedAt = DateTime.UtcNow },
            new User { Id = AgentId,    Username = "agent1",    PasswordHash = "hash", Role = UserRole.Agent,     CreatedAt = DateTime.UtcNow },
            new User { Id = Agent2Id,   Username = "agent2",    PasswordHash = "hash", Role = UserRole.Agent,     CreatedAt = DateTime.UtcNow },
            new User { Id = SubmitterId, Username = "submitter", PasswordHash = "hash", Role = UserRole.Submitter, CreatedAt = DateTime.UtcNow }
        );
        _context.SaveChanges();
    }

    // ─── Existing tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_ShouldReturnTicketWithCorrectDetails()
    {
        var request = new CreateTicketRequest("Test Ticket", "Test description");

        var result = await _service.CreateAsync(request, createdById: AgentId);

        Assert.NotNull(result);
        Assert.Equal("Test Ticket", result.Title);
        Assert.Equal("Test description", result.Description);
        Assert.Equal("Open", result.Status);
        Assert.Equal(AgentId, result.CreatedById);
        Assert.Null(result.AssignedToId);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNullForNonExistentTicket()
    {
        var result = await _service.GetByIdAsync(9999);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldCreateHistoryEntry()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Original Title"), AgentId);
        var updateRequest = new UpdateTicketRequest(Title: "New Title", null, null, null);

        var updated = await _service.UpdateAsync(ticket.Id, updateRequest, changedById: AgentId);

        Assert.NotNull(updated);
        Assert.Equal("New Title", updated.Title);
        Assert.Single(updated.History);
        Assert.Equal("Title", updated.History.First().FieldChanged);
        Assert.Equal("Original Title", updated.History.First().OldValue);
        Assert.Equal("New Title", updated.History.First().NewValue);
    }

    // ─── Role-based filtering tests ────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_SubmitterShouldOnlySeeOwnTickets()
    {
        await _service.CreateAsync(new CreateTicketRequest("Agent Ticket"), createdById: AgentId);
        await _service.CreateAsync(new CreateTicketRequest("Submitter Ticket"), createdById: SubmitterId);

        var results = (await _service.GetAllAsync(SubmitterId, UserRole.Submitter)).ToList();

        Assert.Single(results);
        Assert.Equal("Submitter Ticket", results[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_AgentShouldSeeAssignedAndUnassignedTickets()
    {
        // Unassigned ticket
        var unassigned = await _service.CreateAsync(new CreateTicketRequest("Unassigned"), createdById: SubmitterId);

        // Ticket assigned to our agent
        var assigned = await _service.CreateAsync(new CreateTicketRequest("Assigned to agent1"), createdById: SubmitterId);
        await _service.AssignAsync(assigned.Id, AgentId, AdminId);

        // Ticket assigned to a different agent — should NOT appear
        var otherAgent = await _service.CreateAsync(new CreateTicketRequest("Assigned to agent2"), createdById: SubmitterId);
        await _service.AssignAsync(otherAgent.Id, Agent2Id, AdminId);

        var results = (await _service.GetAllAsync(AgentId, UserRole.Agent)).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Title == "Unassigned");
        Assert.Contains(results, r => r.Title == "Assigned to agent1");
        Assert.DoesNotContain(results, r => r.Title == "Assigned to agent2");
    }

    [Fact]
    public async Task GetAllAsync_AdminShouldSeeAllTickets()
    {
        await _service.CreateAsync(new CreateTicketRequest("Ticket 1"), createdById: SubmitterId);
        var t2 = await _service.CreateAsync(new CreateTicketRequest("Ticket 2"), createdById: SubmitterId);
        await _service.AssignAsync(t2.Id, AgentId, AdminId);

        var results = (await _service.GetAllAsync(AdminId, UserRole.Admin)).ToList();

        Assert.Equal(2, results.Count);
    }

    // ─── Assign tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignAsync_ShouldSetAssignedToIdAndCreateHistory()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("New Ticket"), SubmitterId);
        Assert.Null(ticket.AssignedToId);

        var result = await _service.AssignAsync(ticket.Id, AgentId, AdminId);

        Assert.NotNull(result);
        Assert.Equal(AgentId, result.AssignedToId);
        Assert.Single(result.History);
        Assert.Equal("AssignedToId", result.History.First().FieldChanged);
        Assert.Null(result.History.First().OldValue);
        Assert.Equal(AgentId.ToString(), result.History.First().NewValue);
    }

    [Fact]
    public async Task AssignAsync_ShouldReturnNullForNonExistentTicket()
    {
        var result = await _service.AssignAsync(9999, AgentId, AdminId);
        Assert.Null(result);
    }

    // ─── Update history tests ──────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_StatusChange_ShouldCreateHistoryEntry()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Status Test"), AgentId);
        var updateRequest = new UpdateTicketRequest(null, null, TicketStatus.InProgress, null);

        var updated = await _service.UpdateAsync(ticket.Id, updateRequest, AgentId);

        Assert.NotNull(updated);
        Assert.Equal("InProgress", updated.Status);
        Assert.Single(updated.History);
        Assert.Equal("Status", updated.History.First().FieldChanged);
        Assert.Equal("Open", updated.History.First().OldValue);
        Assert.Equal("InProgress", updated.History.First().NewValue);
    }

    [Fact]
    public async Task UpdateAsync_NoChange_ShouldNotCreateHistoryEntry()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("No Change"), AgentId);
        var updateRequest = new UpdateTicketRequest(null, null, null, null);

        var updated = await _service.UpdateAsync(ticket.Id, updateRequest, AgentId);

        Assert.NotNull(updated);
        Assert.Empty(updated.History);
    }

    // ─── Stats tests ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatsAsync_ShouldCountByStatus()
    {
        await _service.CreateAsync(new CreateTicketRequest("Open 1"), AgentId);
        var t2 = await _service.CreateAsync(new CreateTicketRequest("InProgress"), AgentId);
        await _service.UpdateAsync(t2.Id, new UpdateTicketRequest(null, null, TicketStatus.InProgress, null), AgentId);

        var stats = await _service.GetStatsAsync(AdminId, UserRole.Admin);

        Assert.Equal(1, stats.OpenCount);
        Assert.Equal(1, stats.InProgressCount);
        Assert.Equal(2, stats.TotalVisible);
    }

    // ─── Comment tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AddCommentAsync_ShouldReturnCommentWithAuthor()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Comment Test"), SubmitterId);

        var comment = await _service.AddCommentAsync(ticket.Id, new AddCommentRequest("Hello!"), AgentId);

        Assert.NotNull(comment);
        Assert.Equal("Hello!", comment.Content);
        Assert.Equal("agent1", comment.Author);
        Assert.Equal(AgentId, comment.AuthorId);
    }

    [Fact]
    public async Task AddCommentAsync_ShouldReturnNullForNonExistentTicket()
    {
        var result = await _service.AddCommentAsync(9999, new AddCommentRequest("test"), AgentId);
        Assert.Null(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
