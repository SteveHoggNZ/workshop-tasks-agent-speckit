using CRUDTasksWithAgent.Models;

namespace CRUDTasksWithAgent.Services;

// Filters combine with AND. UrgencyOrder false keeps creation (Id) order.
public record TaskQuery(
    TaskPriority? Priority = null,
    DateOnly? DueOnOrBefore = null,
    bool OverdueOnly = false,
    bool UrgencyOrder = true);
