# C# N8n Workflow Generator

This is a C# console application designed to generate n8n workflow JSON, primarily focusing on the Retrieval Augmented Generation (RAG) pattern. It utilizes Large Language Models (LLMs) through configurable providers like Google Gemini or a local Ollama instance.

## Features

*   Generates n8n workflow JSON based on user's textual descriptions.
*   Supports AI-driven generation via:
    *   Google Gemini API
    *   Local LLMs through Ollama (e.g., Qwen2, Llama3, Mistral)
*   Configuration managed via `appsettings.json` for LLM providers, API keys, and model names.
*   Includes structural validation for the generated n8n JSON.
*   Command-line interface for easy interaction.

## Requirements

*   **.NET 8 SDK** (or the version specified in `N8nWorkflowGenerator.csproj`).
*   **For Google Gemini:**
    *   A valid Google Gemini API Key.
    *   Ensure the Gemini API is enabled for your Google Cloud project.
*   **For Ollama:**
    *   Ollama installed and running. (Download from [ollama.com](https://ollama.com/))
    *   The desired model pulled via Ollama CLI (e.g., `ollama pull qwen2`, `ollama pull llama3`).

## Setup and Configuration

1.  **Clone the Repository (if applicable) or ensure you have the source files.**
    ```bash
    # git clone <repository_url>
    # cd <project_directory>
    ```

2.  **Configure `appsettings.json`:**
    Create or update the `appsettings.json` file in the project's output directory (e.g., `bin/Debug/net8.0`). A template is provided in the source.

    ```json
    {
      "LLM": {
        "Provider": "Ollama", // Choose "Ollama" or "Gemini"
        "Gemini": {
          "ApiKey": "YOUR_GEMINI_API_KEY_HERE", // Replace with your actual Gemini API Key
          "Model": "gemini-1.5-flash-latest" // Or other compatible Gemini model
        },
        "Ollama": {
          "BaseUrl": "http://localhost:11434", // Default Ollama API URL
          "Model": "qwen2" // Replace with your desired Ollama model (e.g., llama3, mistral)
        }
      }
    }
    ```

    *   **`LLM.Provider`**: Set to `"Gemini"` to use Google Gemini or `"Ollama"` to use a local Ollama instance.
    *   **`LLM.Gemini.ApiKey`**: Your Google Gemini API key. **Important:** Do not commit your actual API key to version control if this is a public repository. Use user secrets or environment variables for better security in such cases.
    *   **`LLM.Gemini.Model`**: The specific Gemini model you want to use (e.g., `gemini-1.5-flash-latest`, `gemini-pro`).
    *   **`LLM.Ollama.BaseUrl`**: The base URL for your Ollama API. Defaults to `http://localhost:11434`.
    *   **`LLM.Ollama.Model`**: The name of the model you have pulled and want to use with Ollama (e.g., `qwen2`, `llama3`).

3.  **Development Overrides (Optional):**
    You can create an `appsettings.Development.json` file to override settings for local development. This file is typically not committed to source control if it contains sensitive data.

## How to Run

Navigate to the project's root directory (where `N8nWorkflowGenerator.csproj` is located) in your terminal and run:

```bash
dotnet run
```

The application will start, load the configuration, and prompt you for a description of the n8n workflow you want to create.

## Example Usage

1.  **Run the application:**
    ```bash
    dotnet run
    ```

2.  **Application Output & User Input:**
    ```
    Using Ollama LLM Service (Model: qwen2, URL: http://localhost:11434).

    Welcome to the N8n Workflow Generator!
    This tool will help you generate an n8n workflow JSON based on your description.
    It primarily focuses on creating a Retrieval Augmented Generation (RAG) pattern.
    ----------------------------------------------------------------------------
    Please describe the n8n agent functionality you want to create: Create an n8n workflow named 'Support Ticket Analyzer' that uses Cohere for embeddings, Supabase for vector storage, and the configured Ollama model (qwen2) for chat. It should log to Google Sheets and send Slack alerts on error.
    ```

3.  **LLM Prompt (Printed by the application for review):**
    ```
    --- Constructed LLM Prompt (C#) ---
    Prompt for 'Support Ticket Analyzer' constructed. Length: XXXX chars.
    User Goal: Create an n8n workflow named 'Support Ticket Analyzer' that uses Cohere for embeddings, Supabase for vector storage, and the configured Ollama model (qwen2) for chat. It should log to Google Sheets and send Slack alerts on error.
    LLM Provider for Chat: Ollama, Model: qwen2
    Embedding Service: Cohere, Vector Store: Supabase
    --- End of LLM Prompt Snippet (C#) ---
    ```
    *(Note: The full detailed prompt, as shown in previous Python examples, is constructed but only a summary is printed to the console by default in the C# version to keep output concise. The actual detailed prompt sent to the LLM is much longer and guides the LLM on JSON structure, node types, connections, etc.)*


4.  **Output (if successful):**
    ```
    Attempting to generate workflow... (This might take a moment depending on the LLM response time)

    Validating generated workflow...

    Workflow generated and validated successfully!

    --- Generated Workflow JSON ---
    {
      "name": "Support Ticket Analyzer",
      "nodes": [
        // ... array of nodes generated by the LLM ...
      ],
      "connections": {
        // ... connections generated by the LLM ...
      },
      "settings": {
        "executionOrder": "v1"
      },
      "triggerCount": 1,
      "versionId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" // Example VersionId
    }
    --- End of Workflow JSON ---

    Save workflow to a file? (y/n): y
    Enter filename (default: support-ticket-analyzer.json):
    Workflow saved to C:\path\to\project\bin\Debug\net8.0\support-ticket-analyzer.json
    ```

## Project Structure Overview

*   `Program.cs`: Main entry point for the console application. Orchestrates the workflow.
*   `AppSettings.cs`: Defines C# classes for mapping `appsettings.json` configuration.
*   `ILlmService.cs`: Interface for LLM service abstraction.
*   `GeminiLlmService.cs`: Implementation of `ILlmService` for Google Gemini.
*   `OllamaLlmService.cs`: Implementation of `ILlmService` for Ollama.
*   `WorkflowGenerator.cs`: Handles prompt construction and orchestrates LLM interaction.
*   `UserInputParser.cs`: Parses the user's initial textual description.
*   `N8nWorkflowModels.cs`: Defines C# classes representing the n8n workflow structure.
*   `WorkflowValidator.cs`: Validates the structure of the JSON generated by the LLM.
*   `Helpers.cs`: Utility functions (e.g., Slugify, GenerateUuid).
*   `appsettings.json`: Configuration file for LLM providers, API keys, etc.

## Troubleshooting & Notes

*   **Ollama Users:** Ensure your Ollama server is running and accessible at the `BaseUrl` specified in `appsettings.json`. Make sure the model specified in `Ollama.Model` has been pulled (e.g., `ollama pull qwen2`).
*   **Gemini Users:** Double-check your API key and ensure the Gemini API is enabled in your Google Cloud project. The model name should also be accurate.
*   **LLM Output:** The quality of the generated n8n JSON heavily depends on the capability of the chosen LLM and the detail of the prompt. The prompt constructed by this tool is designed to be comprehensive for RAG workflows.
*   **Rate Limits:** Be mindful of API rate limits if using cloud-based LLMs like Gemini.
```
