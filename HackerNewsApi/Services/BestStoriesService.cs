using HackerNewsApi.Clients;
using HackerNewsApi.Dtos;
using HackerNewsApi.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HackerNewsApi.Services
{
    public class BestStoriesService : IBestStoriesService
    {
        private const string CacheKey = "best-stories";
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);
        private static readonly SemaphoreSlim _cacheLock = new(1, 1);

        private readonly IHackerNewsClient _hackerNewsClient;
        private readonly IMemoryCache _cache;
        private readonly SemaphoreSlim _semaphore = new(10);

        public BestStoriesService(IHackerNewsClient hackerNewsClient, IMemoryCache memoryCache)
        {
            _hackerNewsClient = hackerNewsClient;
            _cache = memoryCache;
        }

        public async Task<IReadOnlyList<BestStoryDto>> GetBestStoriesAsync(int count, CancellationToken ct)
        {
            if (count <= 0)
                return [];

            await _cacheLock.WaitAsync(ct);
            try
            {
                count = Math.Min(count, 100);

                if (_cache.TryGetValue(CacheKey, out List<BestStoryDto>? cached) && cached?.Count >= count)
                    return cached.Take(count).ToList();

                var ids = await _hackerNewsClient.GetBestStoryIdsAsync(ct);
                var idsToFetch = ids.Take(100).ToList();

                var tasks = idsToFetch.Select(i => FetchItemAsync(i, ct));
                var items = await Task.WhenAll(tasks);
                var stories = items
                                .Where(i => i is not null)
                                .Select(Map!)
                                .OrderByDescending(i => i.Score)
                                .ToList();

                _cache.Set(CacheKey, stories, CacheTtl);
                return stories;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async Task<HackerNewsItem> FetchItemAsync(int id, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                return await _hackerNewsClient.GetItemAsync(id, ct);
            }
            finally
            {
                _semaphore.Release(); 
            }
        }

        private static BestStoryDto Map(HackerNewsItem item)
        {
            return new BestStoryDto
                (
                    Title: item.Title ?? "",
                    Uri: item.Url ?? "",
                    PostedBy: item.By ?? "",
                    Time: DateTimeOffset.FromUnixTimeSeconds(item.Time).UtcDateTime,
                    Score: item.Score,
                    CommentCount: item.Descendants
                );
        }
    }
}
