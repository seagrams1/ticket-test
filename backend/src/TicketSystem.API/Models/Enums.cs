namespace TicketSystem.API.Models;

public enum UserRole
{
    Submitter = 0,
    Agent = 1,
    Admin = 2
}

public enum TicketStatus
{
    Open = 0,
    InProgress = 1,
    Paused = 2,
    Resolved = 3,
    Unresolved = 4
}
