using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using DotnetGeminiSDK;

namespace stream_processor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a configuration object
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddEnvironmentVariables()  
                .Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, configuration);

            var serviceProvider = serviceCollection.BuildServiceProvider();

            // Get the StreamProcessor service
            var streamProcessor = serviceProvider.GetRequiredService<StreamProcessor>();
            await streamProcessor.StartKafkaStreamAsync();
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient(); 

            services.AddSingleton<IConfiguration>(configuration);

            // Register GeminiService as the implementation of IMessageProcessingService
            services.AddSingleton<IMessageProcessingService, GeminiService>();

            // Register StreamProcessor for DI
            services.AddSingleton<StreamProcessor>();

            // Configure the GeminiClient
            services.AddGeminiClient(config =>
            {
                config.ApiKey = configuration["GEMINI_API_KEY"] ?? throw new InvalidOperationException();
                config.TextBaseUrl = configuration["GEMINI_TEXT_BASE_URL"] ?? throw new InvalidOperationException();
            });
        }
    }
}