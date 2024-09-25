using Streamiz.Kafka.Net;
using Streamiz.Kafka.Net.SerDes;

namespace stream_processor
{
    public class StreamProcessor(IMessageProcessingService messageProcessingService)
    {
        public async Task StartKafkaStreamAsync()
        {
            // Kafka Streams Configuration
            var config = new StreamConfig<StringSerDes, StringSerDes>
            {
                ApplicationId = "stream-processor-app",
                BootstrapServers = "kafka:9092"
            };

            // Stream Processing with message processing API call
            var builder = new StreamBuilder();
            builder.Stream<string, string>("slack_messages")
                .MapValues(value => messageProcessingService.ProcessTextAsync(value).Result)  
                .To("results");

            // Start Kafka Streams
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
}