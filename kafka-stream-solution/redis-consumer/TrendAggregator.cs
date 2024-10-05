using System.Text.Json;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace redis_consumer;

/**
 * In this architecture, the TrendAggregator effectively fills the role of a Windowing Function in a stream processing system.
 * The TrendAggregator simulates windowing behavior by running a scheduled job (ExecuteAsync method)
 * that wakes up every 30s (30s tumbling window) to process the latest data, essentially providing manual windowing.
 */
public class TrendAggregator(string redisConnectionString) : BackgroundService
{
    private readonly IConnectionMultiplexer _redis = ConnectionMultiplexer.Connect(redisConnectionString);
    private const string TrendingTopicsSortedSetName = "trending-topics";
    private const string SentimentAveragesHashName = "sentiment-averages";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)

    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Simulate a 30s "tumbling window"
            Console.WriteLine("Starting aggregation for the 30s window...");

            await AggregateAndIdentifyTrends();

            Console.WriteLine("Completed aggregation for the 30s window.");

            // Wait for the next 30s window
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); // Run every 30s
        }
    }

    private async Task AggregateAndIdentifyTrends()
    {
        var db = _redis.GetDatabase();

        /* Uncomment the line below for true windowing behavior.
           Without this line, the trending topics will accumulate in a rolling total over time,
           which is suitable for demonstration purposes but may not reflect the requirements of a real-world implementation. */
        //await db.KeyDeleteAsync(TrendingTopicsSortedSetName);

        var allThemes = await db.HashKeysAsync(SentimentAveragesHashName);

        foreach (var theme in allThemes)
        {
            var statsJson = await db.HashGetAsync(SentimentAveragesHashName, theme);
            var stats = JsonSerializer.Deserialize<SentimentStats>(statsJson);

            var relevance = stats.mentionCount; // This could be a more complex function based on the sentiment stats

            // Update the trending topics sorted set
            await db.SortedSetAddAsync(TrendingTopicsSortedSetName, theme, relevance, CommandFlags.FireAndForget);
        }

        Console.WriteLine("Aggregated sentiment and updated trending topics.");
    }
}