// N8nWorkflowGenerator/GeminiLlmService.cs
using System;
using System.Net.Http;
using System.Net.Http.Json; // For ReadFromJsonAsync, PostAsJsonAsync
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
// using Microsoft.Extensions.Options; // If using IOptions for AppSettings

namespace N8nWorkflowGenerator;

// Basic request/response structures for Gemini (simplified)
// These might need to be more complex based on actual Gemini API requirements
public class GeminiRequest
{
    public Content[] contents { get; set; } = Array.Empty<Content>();
    // Add generationConfig if needed, e.g., for temperature, maxTokens
    // public GenerationConfig generationConfig { get; set; }
}

public class Content
{
    public Part[] parts { get; set; } = Array.Empty<Part>();
}

public class Part
{
    public string text { get; set; } = string.Empty;
}

public class GeminiResponse
{
    public Candidate[]? candidates { get; set; }
    // Add promptFeedback for safety ratings etc. if needed
}

public class Candidate
{
    public Content? content { get; set; }
    // Add finishReason, index, safetyRatings etc.
}


public class GeminiLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiSettings _settings;
    private const string GeminiApiVersion = "v1beta"; // Or v1, check current API version

    public GeminiLlmService(HttpClient httpClient, AppSettings appSettings) // Or IOptions<AppSettings> appSettingsOption
    {
        _httpClient = httpClient;
        // _settings = appSettingsOption.Value?.LLM?.Gemini ?? throw new ArgumentNullException(nameof(appSettingsOption), "Gemini settings cannot be null via IOptions.");
        _settings = appSettings?.LLM?.Gemini ?? throw new ArgumentNullException(nameof(appSettings), "Gemini settings section in AppSettings cannot be null.");


        if (string.IsNullOrEmpty(_settings.ApiKey) || _settings.ApiKey == "YOUR_GEMINI_API_KEY_HERE")
        {
            throw new ArgumentException("Gemini API key is not configured or is using the default placeholder value.", nameof(appSettings));
        }
        if (string.IsNullOrEmpty(_settings.Model))
        {
            throw new ArgumentException("Gemini Model is not configured.", nameof(appSettings));
        }
        // Base address might be configured on HttpClient registration in DI setup
        // _httpClient.BaseAddress = new Uri($"https://generativelanguage.googleapis.com/");
    }

    public async Task<string> GenerateWorkflowAsync(string prompt)
    {
        // Model null check already done in constructor, but good practice if settings could change
        if (string.IsNullOrEmpty(_settings.Model))
        {
             throw new InvalidOperationException("Gemini model is not configured.");
        }

        string apiUrl = $"https://generativelanguage.googleapis.com/{GeminiApiVersion}/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";

        var requestBody = new GeminiRequest
        {
            contents = new Content[]
            {
                new Content { parts = new Part[] { new Part { text = prompt } } }
            }
            // Potentially add generationConfig here
            // generationConfig = new GenerationConfig { temperature = 0.7, maxOutputTokens = 2048 }
        };

        try
        {
            var jsonRequestOptions = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
            // Console.WriteLine($"Sending request to Gemini: {JsonSerializer.Serialize(requestBody, jsonRequestOptions)}"); // For debugging
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(apiUrl, requestBody, jsonRequestOptions);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                // Console.WriteLine($"Gemini API Error: {response.StatusCode} - {errorContent}"); // For debugging
                return $"Error from Gemini API: {response.StatusCode} - {errorContent}";
            }

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>();

            if (geminiResponse?.candidates?.Length > 0 && geminiResponse.candidates[0].content?.parts?.Length > 0)
            {
                return geminiResponse.candidates[0].content.parts[0].text ?? "Error: Gemini response part text was null.";
            }
            string rawResponseForError = await response.Content.ReadAsStringAsync(); // Re-read or store earlier if needed
            return $"Error: No content received from Gemini or unexpected response structure. Raw response: {rawResponseForError.Substring(0, Math.Min(rawResponseForError.Length, 500))}";
        }
        catch (HttpRequestException ex)
        {
            // Log exception details
            return $"Error connecting to Gemini API: {ex.Message}";
        }
        catch (JsonException ex)
        {
            // Log exception details
            return $"Error deserializing Gemini API response: {ex.Message}";
        }
        catch (Exception ex)
        {
            // Log exception details
            return $"An unexpected error occurred with Gemini service: {ex.Message}";
        }
    }
}
