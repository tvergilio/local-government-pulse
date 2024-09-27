using Confluent.Kafka;
using StackExchange.Redis;
using System.Text.Json;

namespace redis_consumer
{
    public class RedisConsumer(string redisConnectionString)
    {
        private readonly IConnectionMultiplexer _redis = ConnectionMultiplexer.Connect(redisConnectionString);

        public async Task StartConsumingAsync(CancellationToken cancellationToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "kafka:9092",
                GroupId = "redis-consumer-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<Ignore, string>(config)
                .SetValueDeserializer(Deserializers.Utf8)
                .Build();

            consumer.Subscribe("processing_results");
            Console.WriteLine("Redis consumer started. Listening for messages...");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = consumer.Consume(cancellationToken);
                    if (consumeResult.IsPartitionEOF)
                    {
                        Console.WriteLine("Reached end of   partition.");
                        continue;
                    }

                    var value = consumeResult.Message.Value;
                    Console.WriteLine($"Received message: {value}");

                    // Deserialise and filter for well-formed results
                    var result = JsonSerializer.Deserialize<Dictionary<string, int>>(value);
                    if (result == null ||
                        result.Count == 0) // Check if deserialization was successful and there are results
                    {
                        Console.WriteLine("Discarding malformed or empty result.");
                        continue; // Discard malformed or empty results
                    }

                    var db = _redis.GetDatabase();

                    foreach (var item in result)
                    {
                        await db.SortedSetAddAsync("sentiment-scores", item.Key, item.Value);
                        Console.WriteLine($"Updated Redis: {item.Key} = {item.Value}");
                    }
                }
                catch (ConsumeException e)
                {
                    Console.WriteLine($"Error consuming message: {e.Error.Reason}");
                }
                catch (JsonException e) // Catch potential JSON deserialisation errors
                {
                    Console.WriteLine($"Error deserializing JSON: {e.Message}");
                }
            }
            Console.WriteLine("Redis consumer stopped.");
        }
    }
}