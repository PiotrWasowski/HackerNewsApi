using HackerNewsApi.Clients;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace HackerNewsApi.Tests.Integration
{
    [TestFixture]
    public class BestStoriesIntegrationTests
    {
        private TestWebApplicationFactory _factory = null;
        private HttpClient _client = null;


        [SetUp]
        public void SetUp()
        {
            _factory = new TestWebApplicationFactory();
            _client = _factory.CreateClient();
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        [Test]
        public async Task GetBestStories_Resturns200AndData()
        {
            _factory.HackerNewsClientMock
                .Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { 1 });

            _factory.HackerNewsClientMock
                .Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Models.HackerNewsItem { Id = 1, Title = "Story", Score = 100 });

            var response = await _client.GetAsync("/api/beststories?n=1");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var json = await response.Content.ReadFromJsonAsync<List<object>>();
            Assert.That(json, Is.Not.Null);
            Assert.That(json!.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task SecondRequest_UsesCache_DoesNotCallClientAgain()
        {
            _factory.HackerNewsClientMock
                .Setup(c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { 1 });

            _factory.HackerNewsClientMock
                .Setup(c => c.GetItemAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Models.HackerNewsItem { Id = 1, Title = "Story", Score = 100 });

            var result1 = await _client.GetAsync("/api/beststories?n=1");
            var result2 = await _client.GetAsync("/api/beststories?n=1");

            _factory.HackerNewsClientMock.Verify(
                c => c.GetBestStoryIdsAsync(It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Test]
        public async Task RateLimiting_Returns429_WhenLimitExceeded()
        {
            var tasks = Enumerable.Range(0, 30)
                .Select(_ => _client.GetAsync("/api/beststories?n=1"));

            var responses = await Task.WhenAll(tasks);

            Assert.That(
                responses.Any(r => r.StatusCode == HttpStatusCode.TooManyRequests),
                Is.True);
        }
    }
}
