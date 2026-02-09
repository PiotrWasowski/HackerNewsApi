using HackerNewsApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HackerNewsApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("best-stories-policy")]
    public class BestStoriesController : ControllerBase
    {
        private readonly IBestStoriesService _beststoriesService;

        public BestStoriesController(IBestStoriesService bestStoriesService)
        {
            _beststoriesService = bestStoriesService;
        }

        [HttpGet]
        public async Task<ActionResult> Get([FromQuery] int n = 10, CancellationToken ct = default)
        {
            var result = await _beststoriesService.GetBestStoriesAsync(n, ct);
            return Ok(result);
        }
    }
}
