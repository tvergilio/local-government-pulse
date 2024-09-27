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
                .MapValues(ExtractJsonFromResponse)
                .Filter((key, value) => !string.IsNullOrEmpty(value))
                .To("processing_results");

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
        
        private static string? ExtractJsonFromResponse(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return null; 
            }

            // Extract JSON content from the message
            var jsonStartIndex = message.IndexOf("`json", StringComparison.Ordinal) + "`json".Length;
            var jsonEndIndex = message.LastIndexOf("```", StringComparison.Ordinal);
            if (jsonStartIndex >= 0 && jsonEndIndex > jsonStartIndex)
            {
                var jsonContent = message.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex).Trim();
                return jsonContent;
            }
            return null;
        }
    }
}