using redis_consumer; 

// Create a CancellationTokenSource for graceful shutdown
using var cts = new CancellationTokenSource();

// Register a handler for Ctrl+C to cancel the token and initiate shutdown
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // Prevent the process from terminating immediately
    cts.Cancel();
};


const string redisConnectionString = "redis:6379"; 

// Create an instance of RedisConsumer
var consumer = new RedisConsumer(redisConnectionString);

// Start consuming messages asynchronously, passing the cancellation token
await consumer.StartConsumingAsync(cts.Token);