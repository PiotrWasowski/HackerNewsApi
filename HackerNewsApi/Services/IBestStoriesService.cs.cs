using HackerNewsApi.Dtos;

namespace HackerNewsApi.Services
{
    public interface IBestStoriesService
    {
        Task<IReadOnlyList<BestStoryDto>> GetBestStoriesAsync(int count, CancellationToken ct);
    }
}
