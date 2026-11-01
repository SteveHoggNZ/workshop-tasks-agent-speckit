namespace CRUDTasksWithAgent.Tests.Helpers;

public sealed class FixedTimeProvider(DateOnly today) : TimeProvider
{
    public DateOnly Today { get; set; } = today;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

    public override DateTimeOffset GetUtcNow() =>
        new(Today.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc));
}
