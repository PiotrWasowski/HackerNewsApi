using HackerNewsApi.Clients;
using HackerNewsApi.Models;

namespace HackerNewsApi.Tests
{
    public class SlowHackerNewsClient : IHackerNewsClient
    {
        public int Calls;

        public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken ct)
        {
            Interlocked.Increment(ref Calls);
            await Task.Delay(200, ct);
            return new[] { 1 };
        }

        public Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken ct)
        {
            return Task.FromResult<HackerNewsItem?>(
                new HackerNewsItem
                {
                    Id = 1,
                    Title = "Story",
                    Score = 100
                });
        }
    }
}
