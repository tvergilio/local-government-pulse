using System.Text;
using System.Text.Json;

namespace stream_processor;

public class LlamaService(HttpClient httpClient) : IMessageProcessingService
{
    public async Task<string> ProcessTextAsync(string message)
    {
        var requestPayload = new
        {
            model = "mistral",
            prompt = $@"
            [INST] <<SYS>> You are an expert at identifying themes and analyzing sentiment from text based on predefined categories. <<SYS>> Below are themes of local government interest: [Road Infrastructure, Public Transportation, Waste Management, Public Safety, Health Services, Education, Housing, Environmental Concerns, Public Participation, Budget Allocation]. Please match the following message to the most relevant themes and provide sentiment scoring (0 to 5) for each theme, with 0 being extremely negative and 5 being extremely positive. Do not return themes that are not present in the message. Message: 'The schools are good.' Return the result in a JSON format with the list of matching themes and sentiment scores for each theme. Do not include \""```json \"" or \""```\"". [/INST]",
            temperature = 0.7,
            top_p = 0.9,
            n = 1,
            stream = false
        };

        var jsonRequest = JsonSerializer.Serialize(requestPayload);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        // Make the request to the LLama API
        var response = await httpClient.PostAsync("http://ollama:11434/api/generate", content);
        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<LlamaResponse>(responseString);

            // Extract the JSON portion from the response field
            if (responseObject != null)
            {
                return ExtractJsonFromResponse(responseObject.response);
            }
        }

        return "Error processing sentiment analysis.";
    }

    private string ExtractJsonFromResponse(string llamaResponse)
    {
        var startIndex = llamaResponse.IndexOf('{');
        var endIndex = llamaResponse.LastIndexOf('}');
        if (startIndex >= 0 && endIndex >= 0 && endIndex > startIndex)
        {
            return llamaResponse.Substring(startIndex, endIndex - startIndex + 1);
        }

        return "Invalid response format.";
    }
}