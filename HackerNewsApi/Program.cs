namespace HackerNewsApi
{
    using HackerNewsApi.Services;
    using HackerNewsApi.Clients;
    using System.Threading.RateLimiting;
    using Polly;
    using Polly.Extensions.Http;
    using Microsoft.Extensions.DependencyInjection;

    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //rate limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddPolicy("best-stories-policy", context =>
                {
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: ip,
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 20,            // max 20 requests
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 4,       // better flow
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 0               // no queue
                            });
                });
            });


            // Add services to the container.
            builder.Services.AddControllers();
            
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Cache
            builder.Services.AddMemoryCache();

            // HttpClient
            builder.Services.AddHttpClient<IHackerNewsClient, HackerNewsClient>(client =>
            {
                client.BaseAddress = new Uri("https://hacker-news.firebaseio.com/v0/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBrakerPolicy());

            builder.Services.AddScoped<IBestStoriesService, BestStoriesService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();

            //app.UseAuthorization();

            app.UseRateLimiter();
            app.MapControllers();

            app.Run();
        }

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                    .HandleTransientHttpError()  //5xx + 408
                    .Or<TaskCanceledException>() // timeout
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: attemt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attemt))
                    );
        }

        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBrakerPolicy()
        {
            return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<TaskCanceledException>()
                    .CircuitBreakerAsync(
                        handledEventsAllowedBeforeBreaking: 5,
                        durationOfBreak: TimeSpan.FromSeconds(30)
                    );
        }
    }
}
