namespace Shared.Constants
{
    public static class NeededReviewTime
    {
        public static readonly TimeSpan Level0 = TimeSpan.FromHours(1);
        public static readonly TimeSpan Level1 = TimeSpan.FromDays(1);
        public static readonly TimeSpan Level2 = TimeSpan.FromDays(5);
        public static readonly TimeSpan Level3 = TimeSpan.FromDays(14);
        public static readonly TimeSpan Level4 = TimeSpan.FromDays(30);
        public static readonly TimeSpan Level5 = TimeSpan.FromDays(90);
        public static readonly TimeSpan NotMemorized = TimeSpan.FromHours(1);

    }
}