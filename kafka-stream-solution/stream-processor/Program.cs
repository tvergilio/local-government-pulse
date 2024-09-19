using Streamiz.Kafka.Net;
using Streamiz.Kafka.Net.SerDes;

namespace stream_processor;

class Program
{
    static async Task Main(string[] args)
    {
        // Step 1: Kafka Streams Configuration
        var config = new StreamConfig<StringSerDes, StringSerDes>
        {
            ApplicationId = "stream-processor-app",
            BootstrapServers = "kafka:9092"
        };

        // Step 2: Stream Topology
        var builder = new StreamBuilder();
        builder.Stream<string, string>("slack_messages")
            .MapValues(value => value.ToUpper())
            .To("results");

        // Step 3: Build and Start Kafka Streams
        var streams = new KafkaStream(builder.Build(), config);
        await streams.StartAsync();

        // Gracefully shut down on Ctrl+C
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            streams.Dispose();
        };
    }
}