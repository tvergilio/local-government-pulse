using System.Text.Json;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace redis_consumer;

public class TrendAggregator : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private const string TrendingTopicsSortedSetName = "trending-topics";
    private const string SentimentAveragesHashName = "sentiment-averages";

    public TrendAggregator(string redisConnectionString)
    {
        _redis = ConnectionMultiplexer.Connect(redisConnectionString);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) 

    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await AggregateAndIdentifyTrends();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Run every minute
        }
    }
    private async Task AggregateAndIdentifyTrends()
    {
        var db = _redis.GetDatabase();

        var allThemes = await db.HashKeysAsync(SentimentAveragesHashName);

        foreach (var theme in allThemes)
        {
            var statsJson = await db.HashGetAsync(SentimentAveragesHashName, theme);
            var stats = JsonSerializer.Deserialize<SentimentStats>(statsJson); 

            var mentionCount = stats.mentionCount;

            // Update the trending topics sorted set
            await db.SortedSetAddAsync(TrendingTopicsSortedSetName, theme, mentionCount, CommandFlags.FireAndForget);
            await db.SortedSetRankAsync(TrendingTopicsSortedSetName, theme, Order.Descending);
        }

        Console.WriteLine("Aggregated sentiment and updated trending topics.");
    }
}