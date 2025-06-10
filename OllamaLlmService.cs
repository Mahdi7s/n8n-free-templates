// N8nWorkflowGenerator/OllamaLlmService.cs
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
// using Microsoft.Extensions.Options; // If using IOptions

namespace N8nWorkflowGenerator;

// Basic request/response structures for Ollama (simplified)
public class OllamaRequest
{
    public string model { get; set; } = string.Empty;
    public string prompt { get; set; } = string.Empty;
    public bool stream { get; set; } = false; // Keep it simple, no streaming for now
    // Add options for temperature, etc. if needed
    // public OllamaOptions options {get; set;}
}

// public class OllamaOptions
// {
//    public double temperature {get; set;} = 0.7;
// }

public class OllamaResponse
{
    public string? model { get; set; }
    public DateTimeOffset created_at { get; set; }
    public string? response { get; set; } // This is the generated text
    public bool? done { get; set; }
    // Other fields like total_duration, load_duration, etc.
}

public class OllamaLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;

    public OllamaLlmService(HttpClient httpClient, AppSettings appSettings) // Or IOptions<AppSettings> appSettingsOption
    {
        _httpClient = httpClient;
        // _settings = appSettingsOption.Value?.LLM?.Ollama ?? throw new ArgumentNullException(nameof(appSettingsOption), "Ollama settings cannot be null via IOptions.");
        _settings = appSettings?.LLM?.Ollama ?? throw new ArgumentNullException(nameof(appSettings), "Ollama settings section in AppSettings cannot be null.");

        if (string.IsNullOrEmpty(_settings.BaseUrl))
        {
            throw new ArgumentException("Ollama BaseUrl is not configured.", nameof(appSettings));
        }
        if (string.IsNullOrEmpty(_settings.Model))
        {
            throw new ArgumentException("Ollama Model is not configured.", nameof(appSettings));
        }
        // The BaseAddress for HttpClient is often set during DI registration.
        // If not, it could be set here: _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        // Ensure BaseUrl does not end with a slash if paths below start with one, or vice-versa.
    }

    public async Task<string> GenerateWorkflowAsync(string prompt)
    {
        // Model null check already done in constructor
        if (string.IsNullOrEmpty(_settings.Model))
        {
             throw new InvalidOperationException("Ollama model is not configured.");
        }

        string apiUrl = $"{_settings.BaseUrl.TrimEnd('/')}/api/generate";

        var requestBody = new OllamaRequest
        {
            model = _settings.Model,
            prompt = prompt,
            stream = false // Assuming non-streaming for simplicity
        };

        // Console.WriteLine($"Sending request to Ollama: {JsonSerializer.Serialize(requestBody)}"); // For debugging
        // Console.WriteLine($"Ollama API URL: {apiUrl}"); // For debugging


        try
        {
            var jsonRequestOptions = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(apiUrl, requestBody, jsonRequestOptions);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                // Console.WriteLine($"Ollama API Error: {response.StatusCode} - {errorContent}"); // For debugging
                return $"Error from Ollama API: {response.StatusCode} - {errorContent}";
            }

            var ollamaResponse = await response.Content.ReadFromJsonAsync<OllamaResponse>();
            return ollamaResponse?.response ?? $"Error: No content received from Ollama or unexpected response structure. Raw response: {await response.Content.ReadAsStringAsync()}";
        }
        catch (HttpRequestException ex)
        {
            // Log exception details
            return $"Error connecting to Ollama API: {ex.Message}. Ensure Ollama is running at {_settings.BaseUrl} and the model '{_settings.Model}' is available.";
        }
        catch (JsonException ex)
        {
            // Log exception details
            return $"Error deserializing Ollama API response: {ex.Message}";
        }
        catch (Exception ex)
        {
            // Log exception details
            return $"An unexpected error occurred with Ollama service: {ex.Message}";
        }
    }
}
