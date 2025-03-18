using Application.DTOs.SingleIdPivotEntities;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;

namespace Endpoint.SignalRHub;

public class VocabHub(ISharedDb sharedDb, IUserLearningService userLearningService) : Hub
{
    private readonly ISharedDb _sharedDb = sharedDb;
    private readonly IUserLearningService _userLearningService = userLearningService;
    public override async Task OnDisconnectedAsync(Exception? exception)
    {

        if (Context.Items.TryGetValue("UserId", out var storedUserId) && storedUserId is Guid userId)
        {
            _sharedDb.RemoveConnectedUser(userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task OnUserConnected(string UserId, bool dataExist)
    {
        if (Guid.TryParse(UserId, out Guid parsedUserId))
        {
            Context.Items["UserId"] = parsedUserId;
            _sharedDb.AddConnectedUser(parsedUserId, Context.ConnectionId);
            var lastSync = _sharedDb.GetLastSyncTime(parsedUserId);

            if (lastSync == null || lastSync.Value < DateTime.UtcNow.AddDays(-1) || dataExist == false)
            {
                var learnings = await _userLearningService.GetUserLearnings(parsedUserId);
                await Clients.Caller.SendAsync("SyncLearning", learnings);
                _sharedDb.UpdateLastSyncTime(parsedUserId);
            }
        }
        return;
    }

    public void OnUserDisconnected(string UserId)
    {
        if (Guid.TryParse(UserId, out Guid parsedUserId))
        {
            _sharedDb.RemoveConnectedUser(parsedUserId);
        }
        return;
    }
}
