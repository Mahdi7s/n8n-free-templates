// N8nWorkflowGenerator/Program.cs
using Microsoft.Extensions.Configuration;
using N8nWorkflowGenerator;
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class Program
{
    public static AppSettings? AppConfiguration { get; private set; }
    private static HttpClient? _httpClient; // Make it static or manage scope appropriately

    public static async Task Main(string[] args)
    {
        // Build configuration (existing code is good)
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        AppConfiguration = configuration.Get<AppSettings>();

        if (AppConfiguration == null || AppConfiguration.LLM == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: LLM Configuration is missing or invalid in appsettings.json. Please check the file.");
            Console.ResetColor();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            return;
        }

        // Initialize HttpClient (reuse instance)
        _httpClient = new HttpClient();

        // Instantiate the correct LLM service
        ILlmService? llmService = null;
        try
        {
            if (AppConfiguration.LLM.Provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                if (AppConfiguration.LLM.Gemini == null || string.IsNullOrEmpty(AppConfiguration.LLM.Gemini.ApiKey) || AppConfiguration.LLM.Gemini.ApiKey == "YOUR_GEMINI_API_KEY_HERE") {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Gemini provider is selected, but API key is missing or is a placeholder in appsettings.json.");
                    Console.ResetColor();
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                    return;
                }
                Console.WriteLine($"Using Gemini LLM Service (Model: {AppConfiguration.LLM.Gemini.Model}).");
                llmService = new GeminiLlmService(_httpClient, AppConfiguration);
            }
            else if (AppConfiguration.LLM.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                 if (AppConfiguration.LLM.Ollama == null) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Ollama provider is selected, but Ollama settings are missing in appsettings.json.");
                    Console.ResetColor();
                    Console.WriteLine("Press any key to exit.");
                    Console.ReadKey();
                    return;
                }
                Console.WriteLine($"Using Ollama LLM Service (Model: {AppConfiguration.LLM.Ollama.Model}, URL: {AppConfiguration.LLM.Ollama.BaseUrl}).");
                llmService = new OllamaLlmService(_httpClient, AppConfiguration);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Unknown LLM Provider '{AppConfiguration.LLM.Provider}' in appsettings.json. Supported providers are 'Gemini' or 'Ollama'.");
                Console.ResetColor();
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }
        }
        catch (ArgumentException ex) // Catch specific exceptions from service constructors
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Configuration Error: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            return;
        }
        catch (Exception ex) // Catch any other unexpected errors during LLM service setup
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An unexpected error occurred during LLM service initialization: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            return;
        }


        // --- Main Application Flow ---
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\nWelcome to the N8n Workflow Generator!");
        Console.ResetColor();
        Console.WriteLine("This tool will help you generate an n8n workflow JSON based on your description.");
        Console.WriteLine("It primarily focuses on creating a Retrieval Augmented Generation (RAG) pattern.");
        Console.WriteLine("----------------------------------------------------------------------------");

        Console.Write("Please describe the n8n agent functionality you want to create: ");
        string? userDescription = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userDescription))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No description provided. Exiting.");
            Console.ResetColor();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            return;
        }

        try
        {
            var workflowGenerator = new WorkflowGenerator(llmService, AppConfiguration);

            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("\nAttempting to generate workflow... (This might take a moment depending on the LLM response time)");
            Console.ResetColor();

            string generatedJsonString = await workflowGenerator.GenerateWorkflowJsonAsync(userDescription);

            Console.WriteLine("Validating generated workflow...");
            var (isValid, workflowObject, errors) = WorkflowValidator.ValidateWorkflowJson(generatedJsonString);

            if (isValid && workflowObject != null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\nWorkflow generated and validated successfully!");
                Console.ResetColor();

                var options = new JsonSerializerOptions { WriteIndented = true /*, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping */ };
                string prettyJson = JsonSerializer.Serialize(workflowObject, options);

                Console.WriteLine("\n--- Generated Workflow JSON ---");
                Console.WriteLine(prettyJson);
                Console.WriteLine("--- End of Workflow JSON ---\n");

                Console.Write("Save workflow to a file? (y/n): ");
                string? saveChoice = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (saveChoice == "y")
                {
                    string defaultFileName = $"{Helpers.Slugify(workflowObject.Name)}.json";
                    Console.Write($"Enter filename (default: {defaultFileName}): ");
                    string? userFileName = Console.ReadLine()?.Trim();
                    string finalFileName = string.IsNullOrWhiteSpace(userFileName) ? defaultFileName : userFileName;

                    try
                    {
                        await File.WriteAllTextAsync(finalFileName, prettyJson);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Workflow saved to {Path.GetFullPath(finalFileName)}");
                        Console.ResetColor();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Error saving file: {ex.Message}");
                        Console.ResetColor();
                    }
                }
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\nWorkflow validation failed.");
                Console.ResetColor();
                errors?.ForEach(e => Console.WriteLine($"- {e}"));

                Console.WriteLine("\n--- Faulty LLM Output (Raw) ---");
                Console.WriteLine(generatedJsonString);
                Console.WriteLine("--- End of Faulty LLM Output ---");
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"An error occurred during workflow generation: {ex.Message}");
            if (ex.InnerException != null) {
                 Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            Console.ResetColor();
            Console.WriteLine("Please check your LLM provider settings (API key, model name, Ollama server status) and network connection.");
        }
        finally
        {
            _httpClient?.Dispose();
            Console.WriteLine("\nApplication finished. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
