namespace stream_processor;

public interface IMessageProcessingService
{
    Task<string> ProcessTextAsync(string message);
}