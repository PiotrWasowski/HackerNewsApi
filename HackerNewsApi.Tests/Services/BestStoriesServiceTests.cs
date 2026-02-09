using HackerNewsApi.Clients;
using HackerNewsApi.Dtos;
using HackerNewsApi.Services;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace HackerNewsApi.Tests.Services
{
    [TestFixture]
    public class BestStoriesServiceTests
    {
        private Mock<IHackerNewsClient> _hackerNewsClientMock = null;
        private BestStoriesService _bestStoriesService = null;
        private IMemoryCache _memoryCache = null;
            
        [SetUp]
        public void SetUp()
        {
            _hackerNewsClientMock = new Mock<IHackerNewsClient>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _bestStoriesService = new BestStoriesService(_hackerNewsClientMock.Object, _memoryCache);
        }

        [Test]
        public async Task GetBestStoriesAsync_WhenCountIsZero_ReturnsEmptyList()
        {
            var result = await _bestStoriesService.GetBestStoriesAsync(0, CancellationToken.None);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetBestStoriesAsync_ReturnsStoriesSortedByScoreDescending()
        {
            _hackerNewsClientMock
                .Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { 1, 2 });

            _hackerNewsClientMock
                .Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Models.HackerNewsItem() { Id = 1, Score=100, Title="Low" });

            _hackerNewsClientMock
                .Setup(c => c.GetItemAsync(2, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Models.HackerNewsItem() { Id = 2, Score = 200, Title = "Hight" });

            var result = await _bestStoriesService.GetBestStoriesAsync(2, CancellationToken.None);

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Title, Is.EqualTo("Hight"));
            Assert.That(result[1].Title, Is.EqualTo("Low"));
        }

        [Test]
        public async Task GetBestStoriesAsync_WhenCacheHit_DoesNotCallClientAgain()
        {
            _hackerNewsClientMock
                .Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { 1 });

            _hackerNewsClientMock
                .Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Models.HackerNewsItem() { Id = 1, Score = 100, Title = "NotFromCache" });

            
            var cashedStories = new List<BestStoryDto>()
            {
                new BestStoryDto("CashedStory", "", "", DateTime.UtcNow, 100, 0)
            };

            _memoryCache.Set("best-stories", cashedStories);
            var result = await _bestStoriesService.GetBestStoriesAsync(1, CancellationToken.None);

            Assert.That(result, Is.Not.Empty);
            Assert.That(result[0].Title, Is.Not.EqualTo("NotFromCache"));
            Assert.That(result[0].Title, Is.EqualTo("CashedStory"));

            _hackerNewsClientMock.Verify(
                c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()),
                Times.Never
            );
        }

        [Test]
        public async Task GetBestStoriesAsync_WhenCacheMiss_AddResultToChache()
        {
            _hackerNewsClientMock
                .Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { 1 });

            _hackerNewsClientMock
                .Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Models.HackerNewsItem() { Id = 1, Title="StoryToCache" });

            var result = await _bestStoriesService.GetBestStoriesAsync(1, CancellationToken.None);
            List<BestStoryDto> list = null;
            var exists = _memoryCache.TryGetValue("best-stories", out list);

            Assert.That(exists, Is.True);
            Assert.That(result, Is.Not.Empty);
            Assert.That(list, Is.Not.Empty);
            Assert.That(result[0].Title, Is.EqualTo(list[0].Title));
        }

        [Test]
        public async Task GetBestStoriesAsync_WhenCallConcurrently_RebuildCacheOnlyOnce()
        {
            var client = new SlowHackerNewsClient();
            var cache = new MemoryCache(new MemoryCacheOptions());
            var service = new BestStoriesService(client, cache);

            var tasks = Enumerable
                            .Range(0, 20)
                            .Select(i => service.GetBestStoriesAsync(1, CancellationToken.None))
                            .ToArray();

            var results = await Task.WhenAll(tasks);

            var clientCallsCount = client.Calls;
            Assert.That(clientCallsCount, Is.EqualTo(1), "Cache should be rebuilt only once");
            Assert.That(results.All(r => r.Count() == 1), Is.True);
            Assert.That(results.All(r => r[0].Title == "Story"), Is.True);
        }
    }
}
