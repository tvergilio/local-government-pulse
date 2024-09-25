using DotnetGeminiSDK.Client.Interfaces;
using Microsoft.Extensions.Logging;

namespace stream_processor
{
    public class GeminiService(IGeminiClient geminiClient, ILogger<GeminiService> logger) : IMessageProcessingService
    {
        public async Task<string> ProcessTextAsync(string message)
        {
            try
            {
                var prompt = @"
                You are an expert at identifying themes and analysing sentiment from text based on predefined categories.

                Below are themes of local government interest:
                [Road Infrastructure, Public Transportation, Waste Management, Public Safety, Health Services, Education, Housing, Environmental Concerns, Public Participation, Budget Allocation].

                Please analyse the following message and:

                1. Identify the most relevant themes present in the message. 

                2. If a direct and clear association between the message and any of the predefined themes cannot be made, return an empty JSON object: `{}`

                3. For each identified theme, provide a sentiment score within the range of 1 to 5, where:
                    * 1 = extremely negative
                    * 2 = negative
                    * 3 = neutral
                    * 4 = positive
                    * 5 = extremely positive

                Return the results in a JSON object with the following structure:

                ```json
                {
                  Theme 1: sentiment_score,
                  Theme 2: sentiment_score, 
                  // ... other relevant themes
                }

                Ensure that:

                    Only themes that are clearly present in the message are included in the output.
                    The sentiment scores accurately reflect the tone of the message in relation to each identified theme.
                    Do not provide any explanations or additional context in the response.

                Message: " + message;
                
                // Call the Gemini API using the text prompt method
                var response = await geminiClient.TextPrompt(prompt);

                if (response is { Candidates.Count: > 0 })
                {
                    var resultText = response.Candidates[0].Content.Parts[0].Text;
                    logger.LogInformation("Successfully processed text with Gemini: {resultText}", resultText);
                    return resultText;
                }

                logger.LogWarning("Received unexpected response from Gemini: Response is null or empty.");
                return "Error processing text: Empty response.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing text with Gemini.");
                return $"Error processing text: {ex.Message}";
            }
        }
    }
}
