using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

// Create the host builder
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Register RedisConsumer and TrendAggregator as hosted services
        services.AddHostedService<RedisConsumer>(sp => new RedisConsumer(redisConnectionString));
        services.AddHostedService<TrendAggregator>(sp => new TrendAggregator(redisConnectionString));

    })
    .Build();

// Start the host and its hosted services
await host.RunAsync();