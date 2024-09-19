using Streamiz.Kafka.Net;
using Streamiz.Kafka.Net.SerDes;

namespace stream_processor
{
    public class StreamProcessor
    {
        private readonly LlamaService _llamaService;

        public StreamProcessor(LlamaService llamaService)
        {
            _llamaService = llamaService;
        }

        public async Task StartKafkaStreamAsync()
        {
            // Kafka Streams Configuration
            var config = new StreamConfig<StringSerDes, StringSerDes>
            {
                ApplicationId = "stream-processor-app",
                BootstrapServers = "kafka:9092"
            };

            // Stream Processing with Llama API call
            var builder = new StreamBuilder();
            builder.Stream<string, string>("slack_messages")
                .MapValues(value => _llamaService.ProcessTextWithLlamaAsync(value).Result)  
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