// N8nWorkflowGenerator/AppSettings.cs
namespace N8nWorkflowGenerator;

public class AppSettings
{
    public LlmSettings? LLM { get; set; }
}

public class LlmSettings
{
    public string Provider { get; set; } = "Ollama"; // Default to Ollama
    public GeminiSettings? Gemini { get; set; }
    public OllamaSettings? Ollama { get; set; }
}

public class GeminiSettings
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gemini-pro";
}

public class OllamaSettings
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen2"; // Default popular model
}
