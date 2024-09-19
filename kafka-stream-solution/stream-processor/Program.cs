using System.Threading.Tasks;

namespace stream_processor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var llamaService = new LlamaService();
            var streamProcessor = new StreamProcessor(llamaService);
            await streamProcessor.StartKafkaStreamAsync();
        }
    }
}