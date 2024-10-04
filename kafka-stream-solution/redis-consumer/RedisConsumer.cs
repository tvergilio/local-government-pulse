using Confluent.Kafka;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace redis_consumer
{
    public class RedisConsumer : IHostedService 
    {
        private readonly ConnectionMultiplexer _redis;
        private const string TrendingTopicsSortedSetName = "trending-topics";
        private const string SentimentAveragesHashName = "sentiment-averages";
        private readonly ConsumerConfig _config;
        private IConsumer<Ignore, string> _consumer;

        public RedisConsumer(string redisConnectionString)
        {
            _redis = ConnectionMultiplexer.Connect(redisConnectionString);
            _config = new ConsumerConfig
            {
                BootstrapServers = "kafka:9092",
                GroupId = "redis-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _consumer = new ConsumerBuilder<Ignore, string>(_config)
                .SetValueDeserializer(Deserializers.Utf8)
                .Build();

            _consumer.Subscribe("processing_results");
            Console.WriteLine("Redis consumer started. Listening for messages...");

            _ = Task.Run(() => ConsumeMessagesAsync(cancellationToken), cancellationToken);

            return Task.CompletedTask;
        }

        private async Task ConsumeMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)

            {
                try
                {
                    var consumeResult = _consumer.Consume(cancellationToken);
                    if (consumeResult.IsPartitionEOF)
                    {
                        Console.WriteLine("Reached the end of partition.");
                        continue;
                    }

                    var value = consumeResult.Message.Value;
                    Console.WriteLine($"Received message: {value}");

                    var result = JsonSerializer.Deserialize<Dictionary<string, int>>(value);
                    if (result == null || result.Count == 0)
                    {
                        Console.WriteLine("Discarding malformed or empty result.");
                        continue;
                    }

                    await UpdateRedis(result);
                }
                catch (ConsumeException e)
                {
                    Console.WriteLine($"Error consuming message: {e.Error.Reason}");
                }
                catch (JsonException e)
                {
                    var modifiedMessage = e.Message.Replace("'s", "'").Replace("JSON", "");
                    Console.WriteLine($"Error deserializing: {modifiedMessage}");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _consumer?.Close(); // Close the consumer gracefully
            _consumer?.Dispose();
            _redis.Dispose(); // Dispose of the Redis connection
            Console.WriteLine("Redis consumer stopped.");
            return Task.CompletedTask;
        }

        private async Task UpdateRedis(Dictionary<string, int> result)
        {
            var db = _redis.GetDatabase();

            foreach (var item in result)
            {
                try
                {
                    // Use a Lua script for atomic updates
                    var script = @"
                redis.call('ZINCRBY', KEYS[1], 1, ARGV[1]);
                local currentStatsJson = redis.call('HGET', KEYS[2], ARGV[1]);
                local currentStats = cjson.decode(currentStatsJson or '{}');
                currentStats.totalSentiment = (currentStats.totalSentiment or 0) + ARGV[2];
                currentStats.mentionCount = (currentStats.mentionCount or 0) + 1;
                redis.call('HSET', KEYS[2], ARGV[1], cjson.encode(currentStats));
            ";

                    var keys = new RedisKey[] { TrendingTopicsSortedSetName, SentimentAveragesHashName };
                    var values = new RedisValue[] { item.Key, item.Value };

                    await db.ScriptEvaluateAsync(script, keys, values);
                    Console.WriteLine($"Updated Redis: {item.Key} (mention count), {item.Key} (sentiment stats)");
                }
                catch (RedisException ex)
                {
                    Console.WriteLine($"Error updating Redis: {ex.Message}");
                    // TODO: add retry logic or other error handling here
                }
            }
        }
    }
}