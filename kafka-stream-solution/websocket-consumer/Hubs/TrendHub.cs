using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace hubs
{
    public class TrendHub : Hub
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IHubContext<TrendHub> _hubContext;

        public TrendHub(IConnectionMultiplexer redis, IHubContext<TrendHub> hubContext)
        {
            _redis = redis;
            _hubContext = hubContext;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            Console.WriteLine("Client connected to TrendHub.");

            var subscriber = _redis.GetSubscriber();
            await subscriber.SubscribeAsync("__keyspace@0__:trending-topics", async (channel, notificationType) =>
            {
                Console.WriteLine($"Received Redis notification: {notificationType} on channel {channel}");
                if (notificationType == "zadd" || notificationType == "zrem" || notificationType == "zincr" || notificationType == "del")
                {
                    await SendTrendingTopics();
                }
                {
                    await SendTrendingTopics();
                }
            });

            await SendTrendingTopics();
        }

        public async Task SendTrendingTopics()
        {
            var db = _redis.GetDatabase();
    
            // Fetch trending topics from the sorted set
            var trendingTopics = await db.SortedSetRangeByRankWithScoresAsync("trending-topics", order: Order.Descending);
            Console.WriteLine($"Retrieved {trendingTopics.Length} trending topics from Redis.");

            var topicsWithSentiment = await Task.WhenAll(trendingTopics.Select(async item =>
            {
                // Fetch sentiment data from the hash in Redis
                var statsJson = await db.HashGetAsync("sentiment-averages", item.Element);
                
                // Check if the JSON retrieved from Redis is null or empty
                if (statsJson.IsNullOrEmpty)
                {
                    Console.WriteLine($"No sentiment data found for theme '{item.Element}'. Skipping this entry.");
                    return null;  // Skip this item if no sentiment data is available
                }

                // Log the JSON data for debugging purposes
                Console.WriteLine($"Fetched sentiment data for theme '{item.Element}': {statsJson}");

                // Attempt to deserialiSe the JSON into a SentimentStats object
                SentimentStats? stats = null;
                try
                {
                    stats = JsonSerializer.Deserialize<SentimentStats>(statsJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to deserialise sentiment data for theme '{item.Element}': {ex.Message}");
                    return null;  // Skip this item if deserialisation fails
                }

                // Ensure that the data has a timestamp
                if (stats?.timestamp == null)
                {
                    Console.WriteLine($"Data for theme '{item.Element}' does not have a timestamp. Skipping this entry.");
                    return null;  // Skip 
                }
                
                // Compute average sentiment and prepare the response object
                var averageSentiment = stats?.mentionCount > 0 ? stats.totalSentiment / stats.mentionCount : 0;

                return new { Theme = item.Element.ToString(), Mentions = item.Score, AverageSentiment = averageSentiment };
            }));

            // Filter out any null entries (where sentiment data was missing or failed to deserialise)
            topicsWithSentiment = topicsWithSentiment.Where(x => x != null).ToArray();
            Console.WriteLine($"Sending {topicsWithSentiment.Length} topics with sentiment to clients.");

            // Send the data to all connected clients
            await _hubContext.Clients.All.SendAsync("ReceiveTrendingTopics", topicsWithSentiment);
        }
    }
}