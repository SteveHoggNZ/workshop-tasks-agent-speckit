namespace CRUDTasksWithAgent.Services;

public static class LabServiceCollectionExtensions
{
    public static IServiceCollection AddLabFeatures(this IServiceCollection services)
    {
        services.AddSingleton<TaskChangeNotifier>();
        services.AddSingleton<TaskStatsCalculator>();
        return services;
    }
}
