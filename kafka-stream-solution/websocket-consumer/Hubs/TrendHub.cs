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

        public TrendHub(IConnectionMultiplexer redis)
        {
            _redis = redis;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            Console.WriteLine("Client connected to TrendHub.");

            var subscriber = _redis.GetSubscriber();
            await subscriber.SubscribeAsync("__keyspace@0__:trending-topics", async (channel, notificationType) =>
            {
                Console.WriteLine($"Received Redis notification: {notificationType} on channel {channel}");
                if (notificationType == "zadd" || notificationType == "zrem")
                {
                    await SendTrendingTopics();
                }
            });

            await SendTrendingTopics();
        }

        public async Task SendTrendingTopics()
        {
            var db = _redis.GetDatabase();
            var trendingTopics = await db.SortedSetRangeByRankWithScoresAsync("trending-topics", order: Order.Descending);
            Console.WriteLine($"Retrieved {trendingTopics.Length} trending topics from Redis.");

            var topicsWithSentiment = trendingTopics.Select(async item =>
            {
                var statsJson = await db.HashGetAsync("sentiment-averages", item.Element);
                var stats = JsonSerializer.Deserialize<SentimentStats>(statsJson);
                var averageSentiment = stats.mentionCount > 0 ? stats.totalSentiment / stats.mentionCount : 0;

                return new { Theme = item.Element.ToString(), Mentions = item.Score, AverageSentiment = averageSentiment };
            }).Select(t => t.Result).ToList();

            Console.WriteLine($"Sending {topicsWithSentiment.Count} topics with sentiment to clients.");
            await Clients.All.SendAsync("ReceiveTrendingTopics", topicsWithSentiment);
        }
    }
}