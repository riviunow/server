using Endpoint.SignalRHub;
using Microsoft.AspNetCore.SignalR;

public class SyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _syncInterval;
    private DateTime _previousSyncDatetime = DateTime.UtcNow;

    public SyncBackgroundService(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _syncInterval = TimeSpan.FromHours(int.Parse(configuration["SyncIntervalInHours"] ?? "24"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var sharedDb = scope.ServiceProvider.GetRequiredService<ISharedDb>();
                var vocabHub = scope.ServiceProvider.GetRequiredService<IHubContext<VocabHub>>();
                var userLearningService = scope.ServiceProvider.GetRequiredService<IUserLearningService>();

                var usersToSync = sharedDb.GetConnectedUsersToSync(_previousSyncDatetime);

                foreach (var user in usersToSync)
                {
                    var learnings = await userLearningService.GetUserLearnings(user.Item1);
                    await vocabHub.Clients.Client(user.Item2).SendAsync("SyncLearning", learnings, stoppingToken);
                    sharedDb.UpdateLastSyncTime(user.Item1);
                }

                _previousSyncDatetime = DateTime.UtcNow;
            }

            await Task.Delay(_syncInterval, stoppingToken);
        }
    }
}
