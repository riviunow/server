using System.Collections.Concurrent;

namespace Endpoint.SignalRHub
{
    public interface ISharedDb
    {
        DateTime? GetLastSyncTime(Guid userId);
        void UpdateLastSyncTime(Guid userId);

        void AddConnectedUser(Guid userId, string connectionId);
        void RemoveConnectedUser(Guid userId);
        IList<(Guid, string)> GetConnectedUsersToSync(DateTime lastSyncThreshold);
    }

    public class SharedDb : ISharedDb
    {
        private readonly ConcurrentDictionary<Guid, DateTime> UserLastSync = new();
        private readonly ConcurrentDictionary<Guid, string> ConnectedUsers = new();

        public DateTime? GetLastSyncTime(Guid userId)
        {
            return UserLastSync.TryGetValue(userId, out var lastSync) ? lastSync : null;
        }

        public void UpdateLastSyncTime(Guid userId)
        {
            UserLastSync[userId] = DateTime.UtcNow;
        }

        public void AddConnectedUser(Guid userId, string connectionId)
        {
            ConnectedUsers[userId] = connectionId;
        }

        public void RemoveConnectedUser(Guid userId)
        {
            ConnectedUsers.TryRemove(userId, out _);
        }

        public IList<(Guid, string)> GetConnectedUsersToSync(DateTime lastSyncThreshold)
        {
            return ConnectedUsers.Keys
                .Where(userId => !UserLastSync.TryGetValue(userId, out var lastSync) || lastSync < lastSyncThreshold)
                .Select(userId => (userId, ConnectedUsers[userId]))
                .ToList();
        }
    }

}