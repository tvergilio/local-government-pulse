using System.Text;
using System.Text.Json;

namespace stream_processor;

public class LlamaService
{
    private readonly HttpClient _httpClient;

    public LlamaService()
    {
        _httpClient = new HttpClient();
    }

    public async Task<string> ProcessTextWithLlamaAsync(string prompt)
    {
        // Prepare the JSON payload for the API request
        var requestPayload = new
        {
            model = "mistral",
            prompt = $"<s>[INST] <<SYS>>\nYou are a sentiment analysis expert who provides sentiment scores on a scale from 0 to 5, where 0 is very negative and 5 is very positive.\n<</SYS>>\nAnalyze the sentiment of this message: \"{prompt}\" and return a JSON object with a single field \"sentiment_score\" containing a number from 0 to 5.\nDo not include any additional text or explanations, just output the JSON object.\n[/INST]",
            temperature = 0.7,
            top_p = 0.9,
            n = 1,
            stream = false
        };

        var jsonRequest = JsonSerializer.Serialize(requestPayload);
        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

        // Make the request to the LLama API
        var response = await _httpClient.PostAsync("http://ollama:11434/api/generate", content);
        if (response.IsSuccessStatusCode)
        {
            var responseString = await response.Content.ReadAsStringAsync();
            var responseObject = JsonSerializer.Deserialize<LlamaResponse>(responseString);

            // Extract the JSON portion from the response field
            if (responseObject != null && responseObject.response != null)
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