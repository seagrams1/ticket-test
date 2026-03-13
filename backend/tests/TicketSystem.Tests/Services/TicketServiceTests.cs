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

    // Helper to create default query params
    private static TicketQueryParams DefaultParams() => new TicketQueryParams();

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

        var results = (await _service.GetAllAsync(DefaultParams(), SubmitterId, UserRole.Submitter)).Items.ToList();

        Assert.Single(results);
        Assert.Equal("Submitter Ticket", results[0].Title);
    }

    [Fact]
    public async Task GetAllAsync_AgentShouldSeeAssignedAndUnassignedTickets()
    {
        var unassigned = await _service.CreateAsync(new CreateTicketRequest("Unassigned"), createdById: SubmitterId);
        var assigned = await _service.CreateAsync(new CreateTicketRequest("Assigned to agent1"), createdById: SubmitterId);
        await _service.AssignAsync(assigned.Id, AgentId, AdminId);
        var otherAgent = await _service.CreateAsync(new CreateTicketRequest("Assigned to agent2"), createdById: SubmitterId);
        await _service.AssignAsync(otherAgent.Id, Agent2Id, AdminId);

        var results = (await _service.GetAllAsync(DefaultParams(), AgentId, UserRole.Agent)).Items.ToList();

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

        var results = (await _service.GetAllAsync(DefaultParams(), AdminId, UserRole.Admin)).Items.ToList();

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

    // ─── Phase 3: Search & Filter tests ────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_SearchByTitle_ShouldReturnMatchingTickets()
    {
        await _service.CreateAsync(new CreateTicketRequest("Login bug fix", "Fix the auth module"), SubmitterId);
        await _service.CreateAsync(new CreateTicketRequest("Payment gateway issue", "Stripe integration"), SubmitterId);
        await _service.CreateAsync(new CreateTicketRequest("Dashboard layout"), SubmitterId);

        var result = await _service.GetAllAsync(new TicketQueryParams(Search: "login"), AdminId, UserRole.Admin);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Login bug fix", result.Items.First().Title);
    }

    [Fact]
    public async Task GetAllAsync_SearchByDescription_ShouldReturnMatchingTickets()
    {
        await _service.CreateAsync(new CreateTicketRequest("Ticket A", "Stripe payment integration"), SubmitterId);
        await _service.CreateAsync(new CreateTicketRequest("Ticket B", "Nothing relevant"), SubmitterId);

        var result = await _service.GetAllAsync(new TicketQueryParams(Search: "stripe"), AdminId, UserRole.Admin);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Ticket A", result.Items.First().Title);
    }

    [Fact]
    public async Task GetAllAsync_FilterByStatus_ShouldReturnOnlyMatchingStatus()
    {
        var t1 = await _service.CreateAsync(new CreateTicketRequest("Open ticket"), SubmitterId);
        var t2 = await _service.CreateAsync(new CreateTicketRequest("InProgress ticket"), SubmitterId);
        await _service.UpdateAsync(t2.Id, new UpdateTicketRequest(null, null, TicketStatus.InProgress, null), AdminId);

        var result = await _service.GetAllAsync(new TicketQueryParams(Status: TicketStatus.Open), AdminId, UserRole.Admin);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Open", result.Items.First().Status);
    }

    [Fact]
    public async Task GetAllAsync_AssignedToMe_ShouldReturnOnlyAssignedTickets()
    {
        var t1 = await _service.CreateAsync(new CreateTicketRequest("My ticket"), SubmitterId);
        await _service.AssignAsync(t1.Id, AgentId, AdminId);
        var t2 = await _service.CreateAsync(new CreateTicketRequest("Other ticket"), SubmitterId);
        await _service.AssignAsync(t2.Id, Agent2Id, AdminId);

        var result = await _service.GetAllAsync(
            new TicketQueryParams(AssignedToMe: true),
            AgentId,
            UserRole.Agent);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("My ticket", result.Items.First().Title);
    }

    [Fact]
    public async Task GetAllAsync_Pagination_ShouldReturnCorrectPage()
    {
        for (int i = 1; i <= 5; i++)
            await _service.CreateAsync(new CreateTicketRequest($"Ticket {i}"), SubmitterId);

        var page1 = await _service.GetAllAsync(new TicketQueryParams(Page: 1, PageSize: 2), AdminId, UserRole.Admin);
        var page2 = await _service.GetAllAsync(new TicketQueryParams(Page: 2, PageSize: 2), AdminId, UserRole.Admin);
        var page3 = await _service.GetAllAsync(new TicketQueryParams(Page: 3, PageSize: 2), AdminId, UserRole.Admin);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count());
        Assert.Equal(2, page2.Items.Count());
        Assert.Equal(1, page3.Items.Count());
    }

    [Fact]
    public async Task GetAllAsync_SearchAppliesRoleScoping_SubmitterOnlySeesOwn()
    {
        await _service.CreateAsync(new CreateTicketRequest("Login issue for submitter"), SubmitterId);
        await _service.CreateAsync(new CreateTicketRequest("Login issue for agent"), AgentId);

        var result = await _service.GetAllAsync(new TicketQueryParams(Search: "login"), SubmitterId, UserRole.Submitter);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Login issue for submitter", result.Items.First().Title);
    }

    // ─── Phase 3: Comment edit/delete authorization tests ──────────────────────

    [Fact]
    public async Task EditCommentAsync_AuthorCanEditOwnComment()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Edit Test"), SubmitterId);
        var comment = await _service.AddCommentAsync(ticket.Id, new AddCommentRequest("Original"), AgentId);

        var result = await _service.EditCommentAsync(
            ticket.Id, comment!.Id,
            new EditCommentRequest("Updated content"),
            AgentId, UserRole.Agent);

        Assert.NotNull(result);
        Assert.Equal("Updated content", result.Content);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task EditCommentAsync_NonAuthorCannotEditComment()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Edit Forbid Test"), SubmitterId);
        var comment = await _service.AddCommentAsync(ticket.Id, new AddCommentRequest("Original"), AgentId);

        // Agent2 tries to edit Agent's comment
        var result = await _service.EditCommentAsync(
            ticket.Id, comment!.Id,
            new EditCommentRequest("Hijacked"),
            Agent2Id, UserRole.Agent);

        Assert.Null(result);
    }

    [Fact]
    public async Task EditCommentAsync_ShouldCreateHistoryEntry()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("History Edit Test"), SubmitterId);
        var comment = await _service.AddCommentAsync(ticket.Id, new AddCommentRequest("Original"), AgentId);

        await _service.EditCommentAsync(ticket.Id, comment!.Id,
            new EditCommentRequest("Updated"), AgentId, UserRole.Agent);

        var updated = await _service.GetByIdAsync(ticket.Id);
        var editHistory = updated!.History.Where(h => h.FieldChanged == "Comment" && h.NewValue == "edited").ToList();
        Assert.NotEmpty(editHistory);
    }

    [Fact]
    public async Task DeleteCommentAsync_AuthorCanDeleteOwnComment()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Delete Test"), SubmitterId);
        var comment = await _service.AddCommentAsync(ticket.Id, new AddCommentRequest("To delete"), AgentId);

        var result = await _service.DeleteCommentAsync(ticket.Id, comment!.Id, AgentId, UserRole.Agent);

        Assert.True(result);
        var updatedTicket = await _service.GetByIdAsync(ticket.Id);
        Assert.Empty(updatedTicket!.Comments);
    }

    [Fact]
    public async Task DeleteCommentAsync_AdminCanDeleteAnyComment()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Admin Delete Test"), SubmitterId);
        var comment = await _service.AddCommentAsync(ticket.Id, new AddCommentRequest("Agent's comment"), AgentId);

        // Admin deletes agent's comment
        var result = await _service.DeleteCommentAsync(ticket.Id, comment!.Id, AdminId, UserRole.Admin);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteCommentAsync_NonAuthorNonAdminCannotDelete()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Delete Forbid Test"), SubmitterId);
        var comment = await _service.AddCommentAsync(ticket.Id, new AddCommentRequest("Agent's comment"), AgentId);

        // Agent2 tries to delete Agent's comment
        var result = await _service.DeleteCommentAsync(ticket.Id, comment!.Id, Agent2Id, UserRole.Agent);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteCommentAsync_ShouldCreateHistoryEntry()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Delete History Test"), SubmitterId);
        var comment = await _service.AddCommentAsync(ticket.Id, new AddCommentRequest("Will be deleted"), AgentId);

        await _service.DeleteCommentAsync(ticket.Id, comment!.Id, AgentId, UserRole.Agent);

        var updated = await _service.GetByIdAsync(ticket.Id);
        var deleteHistory = updated!.History.Where(h => h.FieldChanged == "Comment" && h.NewValue == "deleted").ToList();
        Assert.NotEmpty(deleteHistory);
    }

    [Fact]
    public async Task DeleteCommentAsync_ShouldReturnNullForNonExistentComment()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Test"), SubmitterId);
        var result = await _service.DeleteCommentAsync(ticket.Id, 9999, AdminId, UserRole.Admin);
        Assert.Null(result);
    }

    // ─── Priority tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithHighPriority_ShouldSetPriorityCorrectly()
    {
        var request = new CreateTicketRequest("High Priority Ticket", "Urgent issue", TicketPriority.High);

        var result = await _service.CreateAsync(request, AgentId);

        Assert.NotNull(result);
        Assert.Equal("High", result.Priority);
    }

    [Fact]
    public async Task CreateAsync_DefaultPriority_ShouldBeMedium()
    {
        var request = new CreateTicketRequest("Default Priority Ticket");

        var result = await _service.CreateAsync(request, AgentId);

        Assert.Equal("Medium", result.Priority);
    }

    [Fact]
    public async Task UpdateAsync_PriorityChange_ShouldCreateHistoryEntry()
    {
        var ticket = await _service.CreateAsync(
            new CreateTicketRequest("Priority Test", "desc", TicketPriority.Low),
            AgentId);

        var updated = await _service.UpdateAsync(
            ticket.Id,
            new UpdateTicketRequest(null, null, null, null, TicketPriority.Critical),
            AgentId);

        Assert.NotNull(updated);
        Assert.Equal("Critical", updated.Priority);
        Assert.Single(updated.History);
        var histEntry = updated.History.First();
        Assert.Equal("Priority", histEntry.FieldChanged);
        Assert.Equal("Low", histEntry.OldValue);
        Assert.Equal("Critical", histEntry.NewValue);
    }

    [Fact]
    public async Task UpdateAsync_SamePriority_ShouldNotCreateHistoryEntry()
    {
        var ticket = await _service.CreateAsync(
            new CreateTicketRequest("Same Priority", "desc", TicketPriority.Medium),
            AgentId);

        var updated = await _service.UpdateAsync(
            ticket.Id,
            new UpdateTicketRequest(null, null, null, null, TicketPriority.Medium),
            AgentId);

        Assert.NotNull(updated);
        Assert.Empty(updated.History);
    }

    // ─── Pagination edge cases ─────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_PageOutOfBounds_ShouldReturnEmptyItems()
    {
        for (int i = 1; i <= 3; i++)
            await _service.CreateAsync(new CreateTicketRequest($"Ticket {i}"), SubmitterId);

        // Page 100 is way out of bounds — should return 0 items but correct totalCount
        var result = await _service.GetAllAsync(
            new TicketQueryParams(Page: 100, PageSize: 10),
            AdminId, UserRole.Admin);

        Assert.Equal(3, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_PageSizeZero_ShouldDefaultToOne()
    {
        for (int i = 1; i <= 5; i++)
            await _service.CreateAsync(new CreateTicketRequest($"Ticket {i}"), SubmitterId);

        // pageSize=0 is clamped to 1 by Math.Clamp(pageSize, 1, 100)
        var result = await _service.GetAllAsync(
            new TicketQueryParams(Page: 1, PageSize: 0),
            AdminId, UserRole.Admin);

        Assert.Equal(5, result.TotalCount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAllAsync_NegativePage_ShouldDefaultToPageOne()
    {
        for (int i = 1; i <= 3; i++)
            await _service.CreateAsync(new CreateTicketRequest($"Ticket {i}"), SubmitterId);

        // Negative page is clamped to 1 by Math.Max(1, page)
        var result = await _service.GetAllAsync(
            new TicketQueryParams(Page: -5, PageSize: 10),
            AdminId, UserRole.Admin);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
    }

    // ─── History tracking — all trackable changes ──────────────────────────────

    [Fact]
    public async Task UpdateAsync_MultipleChanges_ShouldCreateMultipleHistoryEntries()
    {
        var ticket = await _service.CreateAsync(
            new CreateTicketRequest("Original Title", "Original Desc"),
            AgentId);

        var updated = await _service.UpdateAsync(
            ticket.Id,
            new UpdateTicketRequest("New Title", "New Desc", TicketStatus.InProgress, null),
            AdminId);

        Assert.NotNull(updated);
        Assert.Equal(3, updated.History.Count());

        var fields = updated.History.Select(h => h.FieldChanged).ToList();
        Assert.Contains("Title", fields);
        Assert.Contains("Description", fields);
        Assert.Contains("Status", fields);
    }

    [Fact]
    public async Task UpdateAsync_DescriptionChange_ShouldCreateHistoryEntry()
    {
        var ticket = await _service.CreateAsync(
            new CreateTicketRequest("Test", "Original description"),
            AgentId);

        var updated = await _service.UpdateAsync(
            ticket.Id,
            new UpdateTicketRequest(null, "Updated description", null, null),
            AgentId);

        Assert.NotNull(updated);
        Assert.Single(updated.History);
        var hist = updated.History.First();
        Assert.Equal("Description", hist.FieldChanged);
        Assert.Equal("Original description", hist.OldValue);
        Assert.Equal("Updated description", hist.NewValue);
    }

    [Fact]
    public async Task AssignAsync_ThenReassign_ShouldTrackBothHistoryEntries()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Reassign Test"), SubmitterId);

        // First assignment
        await _service.AssignAsync(ticket.Id, AgentId, AdminId);

        // Re-assign via UpdateAsync (which tracks AssignedToId changes)
        var updated = await _service.UpdateAsync(
            ticket.Id,
            new UpdateTicketRequest(null, null, null, Agent2Id),
            AdminId);

        Assert.NotNull(updated);
        // Should have 2 history entries: one from AssignAsync, one from UpdateAsync
        Assert.Equal(2, updated.History.Count());
    }

    [Fact]
    public async Task UpdateAsync_AllResolvableStatuses_ShouldBeTracked()
    {
        var ticket = await _service.CreateAsync(new CreateTicketRequest("Status Chain"), AgentId);

        var statuses = new[] { TicketStatus.InProgress, TicketStatus.Paused, TicketStatus.Resolved };
        foreach (var status in statuses)
        {
            await _service.UpdateAsync(ticket.Id, new UpdateTicketRequest(null, null, status, null), AgentId);
        }

        var final = await _service.GetByIdAsync(ticket.Id);
        Assert.NotNull(final);
        Assert.Equal("Resolved", final.Status);

        // Should have 3 history entries for 3 status changes
        var statusHistory = final.History.Where(h => h.FieldChanged == "Status").ToList();
        Assert.Equal(3, statusHistory.Count);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
