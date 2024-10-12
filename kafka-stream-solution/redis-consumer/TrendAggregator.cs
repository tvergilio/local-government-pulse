using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace redis_consumer;

public class TrendAggregator : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private const string TrendingTopicsSortedSetName = "trending-topics";
    private const string SentimentAveragesHashName = "sentiment-averages";
    private byte[]? _luaScriptSha1;
    private const long WindowSize = 60; // Set to 1 minute or 60 seconds

    public TrendAggregator(string redisConnectionString)
    {
        _redis = ConnectionMultiplexer.Connect(redisConnectionString);
        // Load the Lua script and cache the SHA1 hash at startup
        Task.Run(async () => _luaScriptSha1 = await LoadLuaScriptAsync());
    }

    // This method loads the Lua script into Redis and returns the SHA1 hash (as a byte[])
    private async Task<byte[]> LoadLuaScriptAsync()
    {
        var server = _redis.GetServer(_redis.GetEndPoints()[0]);
        const string luaScript = @"
        -- Clear the sorted set before processing new data
        redis.call('DEL', KEYS[2])

        -- Retrieve all themes from the hash
        local themes = redis.call('HKEYS', KEYS[1])

        -- Get the current timestamp (passed in as ARGV[1]) and the window size (ARGV[2])
        local currentTime = tonumber(ARGV[1])
        local windowSize = tonumber(ARGV[2])

        -- Iterate over all themes in the hash
        for i, theme in ipairs(themes) do
            -- Get the sentiment stats for each theme
            local statsJson = redis.call('HGET', KEYS[1], theme)

            -- Ensure that statsJson is not null or empty
            if statsJson and statsJson ~= '' then
                local stats = cjson.decode(statsJson)

                -- Check if the data is within the current window
                if stats.timestamp and tonumber(stats.timestamp) + windowSize >= currentTime then
                    -- Data is within the valid window, so process it

                    -- Compute relevance (based on mentionCount)
                    local relevance = stats.mentionCount or 0

                    -- Only update the sorted set if relevance is greater than 0
                    if relevance > 0 then
                        redis.call('ZADD', KEYS[2], relevance, theme)
                    end
                else
                    -- Data is outside the current window, remove it from the hash
                    redis.call('HDEL', KEYS[1], theme)
                end
            end
        end

        return themes
        ";

        // Load the script into Redis and get its SHA1 hash (as byte[])
        var sha1Bytes = await server.ScriptLoadAsync(luaScript);

        Console.WriteLine($"Lua script loaded with SHA1 hash.");
        return sha1Bytes;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Simulate a "tumbling window"
            Console.WriteLine("Starting aggregation for the " + WindowSize + "s window...");

            await AggregateAndIdentifyTrends();

            Console.WriteLine("Completed aggregation for the " + WindowSize + "s window.");

            await Task.Delay(TimeSpan.FromSeconds(WindowSize), stoppingToken);
        }
    }

    private async Task AggregateAndIdentifyTrends()
    {
        var db = _redis.GetDatabase();

        // Check if the script SHA1 is loaded, reload if necessary
        if (_luaScriptSha1 == null)
        {
            Console.WriteLine("Lua script SHA1 is empty. Reloading the Lua script...");
            _luaScriptSha1 = await LoadLuaScriptAsync();
        }

        // Get the current timestamp (Unix time) and define the window size in seconds (e.g., 60 seconds)
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Execute the cached Lua script using its SHA1 hash, passing the current timestamp and window size as ARGV
        var redisResult = await db.ScriptEvaluateAsync(
            _luaScriptSha1,
            new RedisKey[] { SentimentAveragesHashName, TrendingTopicsSortedSetName },
            new RedisValue[] { currentTime, WindowSize }
        );

        string?[] result = Array.Empty<string>();

        if (redisResult.IsNull)
        {
            Console.WriteLine("No themes were processed.");
        }
        else
        {
            result = ((RedisResult[])redisResult).Select(r => (string)r).ToArray();
            Console.WriteLine($"Processed {result.Length} themes and updated trending topics.");
        }

        Console.WriteLine($"Processed {result.Length} themes, updated trending topics, and reset the sorted set.");
    }
}