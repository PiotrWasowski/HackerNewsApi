using HackerNewsApi.Models;

namespace HackerNewsApi.Clients
{
    public interface IHackerNewsClient
    {
        Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken ct);
        Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken ct);
    }
}