using Confluent.Kafka;
using StackExchange.Redis;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace redis_consumer
{
    public class RedisConsumer : IHostedService 
    {
        private readonly ConnectionMultiplexer _redis;
        private const string SentimentAveragesHashName = "sentiment-averages";
        private readonly ConsumerConfig _config;
        private IConsumer<Ignore, string> _consumer;
        private byte[]? _luaScriptSha1;

        public RedisConsumer(string redisConnectionString)
        {
            _redis = ConnectionMultiplexer.Connect(redisConnectionString);
            _config = new ConsumerConfig
            {
                BootstrapServers = "kafka:9092",
                GroupId = "redis-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            // Load the Lua script and cache the SHA1 hash at startup
            Task.Run(async () => _luaScriptSha1 = await LoadLuaScriptAsync());
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

        /**
         * The Lua script updates the sentiment stats and timestamps in Redis.
         * It is similar to an accumulator in stream processing frameworks.
         */
        private async Task<byte[]> LoadLuaScriptAsync()
        {
            var server = _redis.GetServer(_redis.GetEndPoints()[0]);

            // Lua script to update sentiment stats and timestamps
            const string luaScript = @"
                local currentStatsJson = redis.call('HGET', KEYS[1], ARGV[1]);  
                local currentStats = cjson.decode(currentStatsJson or '{}');
                
                -- Update sentiment stats
                currentStats.totalSentiment = (currentStats.totalSentiment or 0) + tonumber(ARGV[2]);
                currentStats.mentionCount = (currentStats.mentionCount or 0) + 1;
                
                -- Add the timestamp
                currentStats.timestamp = tonumber(ARGV[3]);
                
                -- Save the updated stats back to Redis
                redis.call('HSET', KEYS[1], ARGV[1], cjson.encode(currentStats));
            ";

            // Load the script into Redis and get its SHA1 hash (as byte[])
            var sha1Bytes = await server.ScriptLoadAsync(luaScript);

            Console.WriteLine($"Lua script loaded with SHA1 hash.");
            return sha1Bytes;
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

        /**
         * Updates the Redis sentiment data using the cached Lua script.
         * Timestamps are updated based on processing time.
         */
        private async Task UpdateRedis(Dictionary<string, int> result)
        {
            var db = _redis.GetDatabase();
            
            // Using current time (processing time), but could be replaced with Kafka message timestamp,
            // event time, or any other function
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); 

            // Check if the script SHA1 is loaded, reload if necessary
            if (_luaScriptSha1 == null)
            {
                Console.WriteLine("Lua script SHA1 is empty. Reloading the Lua script...");
                _luaScriptSha1 = await LoadLuaScriptAsync();
            }

            foreach (var item in result)
            {
                try
                {
                    // Pass the theme (item.Key), sentiment value (item.Value), and the current timestamp
                    var keys = new RedisKey[] { SentimentAveragesHashName };
                    var values = new RedisValue[] { item.Key, item.Value.ToString(), currentTime };

                    // Execute the cached Lua script using its SHA1 hash
                    await db.ScriptEvaluateAsync(_luaScriptSha1, keys, values);

                    Console.WriteLine($"Updated Redis: {item.Key} (mention count), {item.Key} (sentiment stats with timestamp)");
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