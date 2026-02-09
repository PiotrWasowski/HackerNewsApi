using HackerNewsApi.Models;
using System.Text.Json;

namespace HackerNewsApi.Clients
{
    public class HackerNewsClient : IHackerNewsClient
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions _jsonOptions =
            new()
            {
                PropertyNameCaseInsensitive = true
            };

        public HackerNewsClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IReadOnlyList<int>> GetBestStoryIdsAsync(CancellationToken ct)
        {
            var response = await _httpClient.GetAsync("beststories.json", ct);
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<List<int>>(stream, cancellationToken: ct) ?? [];
        }

        public async Task<HackerNewsItem?> GetItemAsync(int id, CancellationToken ct)
        {
            var response = await _httpClient.GetAsync($"item/{id}.json", ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<HackerNewsItem>(stream, _jsonOptions, cancellationToken: ct);
        }
    }
}
