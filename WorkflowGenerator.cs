// N8nWorkflowGenerator/WorkflowGenerator.cs
using System;
using System.Text; // For StringBuilder
using System.Text.Json;
using System.Threading.Tasks;

namespace N8nWorkflowGenerator;

public class WorkflowGenerator
{
    private readonly ILlmService _llmService;
    private readonly AppSettings _appSettings;

    public WorkflowGenerator(ILlmService llmService, AppSettings appSettings)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
    }

    // Made public for testing purposes in Program.cs for this subtask, can be private later.
    private string ConstructLlmPrompt(string userFullDescription, WorkflowDetails extractedDetails) // Changed to private
    {
        // This is an adaptation of the prompt engineering strategy from Python Step 2.
        // It should be a comprehensive prompt guiding the LLM to produce n8n JSON.

        string workflowName = extractedDetails.WorkflowName;
        string webhookPath = Helpers.Slugify(workflowName);
        string indexName = Helpers.Slugify(workflowName);

        string embeddingModelName = "default-embedding-model";
        string chatModelName = "default-chat-model";

        if (_appSettings.LLM != null) {
            if (_appSettings.LLM.Provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase) && _appSettings.LLM.Gemini != null) {
                chatModelName = _appSettings.LLM.Gemini.Model;
            } else if (_appSettings.LLM.Provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) && _appSettings.LLM.Ollama != null) {
                chatModelName = _appSettings.LLM.Ollama.Model;
            }
        }

        string embeddingNodeJsonSnippet = "";
        string embeddingCredentialId = "GENERIC_EMBEDDING_API";
        // string embeddingCredentialName = "Generic Embedding Service"; // Not used in prompt directly

        switch (extractedDetails.EmbeddingService.ToLowerInvariant())
        {
            case "openai":
                embeddingNodeJsonSnippet = "\"type\": \"@n8n/n8n-nodes-langchain.embeddingsOpenAi\", \"parameters\": {\"model\": \"text-embedding-3-small\"}";
                embeddingCredentialId = "OPENAI_EMBEDDING_CREDENTIALS_ID"; // Placeholder ID
                embeddingModelName = "text-embedding-3-small";
                break;
            case "cohere":
                embeddingNodeJsonSnippet = "\"type\": \"@n8n/n8n-nodes-langchain.embeddingsCohere\", \"parameters\": {\"model\": \"embed-english-v3.0\"}";
                embeddingCredentialId = "COHERE_EMBEDDING_CREDENTIALS_ID";
                embeddingModelName = "embed-english-v3.0";
                break;
            case "huggingface":
                embeddingNodeJsonSnippet = "\"type\": \"@n8n/n8n-nodes-langchain.embeddingsHuggingFace\", \"parameters\": {\"model\": \"sentence-transformers/all-MiniLM-L6-v2\"}";
                embeddingCredentialId = "HF_EMBEDDING_CREDENTIALS_ID";
                embeddingModelName = "sentence-transformers/all-MiniLM-L6-v2";
                break;
            default:
                 embeddingNodeJsonSnippet = "\"type\": \"@n8n/n8n-nodes-langchain.embeddingsOpenAi\", \"parameters\": {\"model\": \"text-embedding-3-small\"}";
                embeddingCredentialId = "OPENAI_EMBEDDING_CREDENTIALS_ID";
                embeddingModelName = "text-embedding-3-small";
                break;
        }

        string vectorStoreNodeJsonSnippet_Insert = "";
        string vectorStoreNodeJsonSnippet_Query = "";
        string vectorStoreCredentialId = "GENERIC_VECTOR_DB_API";
        // string vectorStoreCredentialName = "Generic Vector DB"; // Not used
        string vectorStoreToolName = extractedDetails.VectorStore; // Used for tool name

        switch (extractedDetails.VectorStore.ToLowerInvariant())
        {
            case "supabase":
                vectorStoreNodeJsonSnippet_Insert = $"\"type\": \"@n8n/n8n-nodes-langchain.vectorStoreSupabase\", \"parameters\": {{ \"mode\": \"insert\", \"indexName\": \"{indexName}\" }}";
                vectorStoreNodeJsonSnippet_Query = $"\"type\": \"@n8n/n8n-nodes-langchain.vectorStoreSupabase\", \"parameters\": {{ \"indexName\": \"{indexName}\" }}";
                vectorStoreCredentialId = "SUPABASE_VECTOR_CREDENTIALS_ID";
                break;
            case "pinecone":
                vectorStoreNodeJsonSnippet_Insert = $"\"type\": \"@n8n/n8n-nodes-langchain.vectorStorePinecone\", \"parameters\": {{ \"mode\": \"insert\", \"pineconeIndex\": \"{indexName}\" }}";
                vectorStoreNodeJsonSnippet_Query = $"\"type\": \"@n8n/n8n-nodes-langchain.vectorStorePinecone\", \"parameters\": {{ \"pineconeIndex\": \"{indexName}\" }}";
                vectorStoreCredentialId = "PINECONE_CREDENTIALS_ID";
                break;
            default:
                vectorStoreNodeJsonSnippet_Insert = $"\"type\": \"@n8n/n8n-nodes-langchain.vectorStoreSupabase\", \"parameters\": {{ \"mode\": \"insert\", \"indexName\": \"{indexName}\" }}";
                vectorStoreNodeJsonSnippet_Query = $"\"type\": \"@n8n/n8n-nodes-langchain.vectorStoreSupabase\", \"parameters\": {{ \"indexName\": \"{indexName}\" }}";
                vectorStoreCredentialId = "SUPABASE_VECTOR_CREDENTIALS_ID";
                break;
        }

        string chatModelNodeJsonSnippet = "";
        string chatModelCredentialId = "GENERIC_CHAT_API";
        // string chatModelCredentialName = "Generic Chat Service"; // Not used

        string configuredLlmProvider = _appSettings.LLM?.Provider ?? "Ollama";
        string configuredLlmModel = chatModelName;

        if (configuredLlmProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase)) {
            chatModelNodeJsonSnippet = $"\"type\": \"@n8n/n8n-nodes-langchain.lmChatGemini\", \"parameters\": {{ \"model\": \"{configuredLlmModel}\" }}";
            chatModelCredentialId = "GEMINI_CHAT_CREDENTIALS_ID";
        } else if (configuredLlmProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)) {
            chatModelNodeJsonSnippet = $"\"type\": \"@n8n/n8n-nodes-langchain.lmChatOllama\", \"parameters\": {{ \"model\": \"{configuredLlmModel}\" }}"; // Assuming lmChatOllama exists
            chatModelCredentialId = "OLLAMA_CHAT_CREDENTIALS_ID"; // Often not directly used in node for Ollama
        } else {
             if(extractedDetails.ChatModelService.ToLowerInvariant() == "openai") {
                chatModelNodeJsonSnippet = $"\"type\": \"@n8n/n8n-nodes-langchain.lmChatOpenAi\", \"parameters\": {{ \"model\": \"gpt-4o\"}}";
                chatModelCredentialId = "OPENAI_CHAT_CREDENTIALS_ID";
             } else if (extractedDetails.ChatModelService.ToLowerInvariant() == "anthropic") {
                chatModelNodeJsonSnippet = $"\"type\": \"@n8n/n8n-nodes-langchain.lmChatAnthropic\", \"parameters\": {{ \"model\": \"claude-3-opus-20240229\"}}";
                chatModelCredentialId = "ANTHROPIC_CHAT_CREDENTIALS_ID";
             } else {
                chatModelNodeJsonSnippet = $"\"type\": \"@n8n/n8n-nodes-langchain.lmChatOpenAi\", \"parameters\": {{ \"model\": \"gpt-3.5-turbo\"}}";
                chatModelCredentialId = "OPENAI_CHAT_CREDENTIALS_ID";
             }
        }

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine($"You are an expert n8n workflow generator. Your task is to create a complete n8n JSON workflow configuration based on the following requirements:");
        promptBuilder.AppendLine($"User's overall goal: {userFullDescription}");
        promptBuilder.AppendLine($"The workflow should be named: \"{workflowName}\"");
        promptBuilder.AppendLine($"It should follow the Standard RAG (Retrieval Augmented Generation) pattern.");
        promptBuilder.AppendLine($"Trigger: Webhook with path \"{webhookPath}\".");
        promptBuilder.AppendLine($"Embedding Service: {extractedDetails.EmbeddingService} (Model: {embeddingModelName}). Node details hint: {embeddingNodeJsonSnippet}. Credential ID Placeholder: {embeddingCredentialId}");
        promptBuilder.AppendLine($"Vector Store: {extractedDetails.VectorStore} (Index Name: {indexName}). Node details hint (Insert): {vectorStoreNodeJsonSnippet_Insert}. Node details hint (Query): {vectorStoreNodeJsonSnippet_Query}. Credential ID Placeholder: {vectorStoreCredentialId}. Tool Name: {vectorStoreToolName}");
        promptBuilder.AppendLine($"Chat Model Service (for RAG Agent): {configuredLlmProvider} (Model: {configuredLlmModel}). Node details hint: {chatModelNodeJsonSnippet}. Credential ID Placeholder: {chatModelCredentialId}");
        promptBuilder.AppendLine($"The RAG Agent should have a system message: \"You are an assistant for {workflowName}\".");
        promptBuilder.AppendLine($"Output Logging: Use Google Sheets (Sheet ID: 'GOOGLE_SHEET_ID_PLACEHOLDER', Sheet Name: 'Log'). The Google Sheets node type is 'n8n-nodes-base.googleSheets'.");
        promptBuilder.AppendLine($"Error Handling: Use Slack (Channel: '#alerts_placeholder'). The Slack node type is 'n8n-nodes-base.slack'.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Please generate the complete n8n JSON. Ensure all node IDs are unique UUIDs. Pay close attention to the 'connections' structure, ensuring correct source and target nodes and connection types (main, ai_textSplitter, ai_embedding, ai_vectorStore, ai_tool, ai_memory, ai_languageModel, onError).");
        promptBuilder.AppendLine("Here is a very basic skeleton of the expected top-level JSON structure:");
        promptBuilder.AppendLine("{\n  \"name\": \"WORKFLOW_NAME_PLACEHOLDER\",\n  \"nodes\": [\n    // ... array of node objects ...\n  ],\n  \"connections\": {\n    // ... connection object ...\n  },\n  \"settings\": {\n    \"executionOrder\": \"v1\"\n  },\n  \"triggerCount\": 1,\n  \"versionId\": \"UUID_PLACEHOLDER\"\n}");
        promptBuilder.AppendLine("Make sure to include the following nodes with appropriate parameters and connections for the RAG pattern:");
        promptBuilder.AppendLine("- Webhook Trigger (n8n-nodes-base.webhook)");
        promptBuilder.AppendLine("- Text Splitter (@n8n/n8n-nodes-langchain.textSplitterCharacterTextSplitter)");
        promptBuilder.AppendLine($"- Embeddings ({extractedDetails.EmbeddingService} - use the specific type like @n8n/n8n-nodes-langchain.embeddingsOpenAi from hint). Credential ID: {embeddingCredentialId}");
        promptBuilder.AppendLine($"- Vector Store Insert ({extractedDetails.VectorStore} - use specific type like @n8n/n8n-nodes-langchain.vectorStoreSupabase from hint, mode: insert). Credential ID: {vectorStoreCredentialId}");
        promptBuilder.AppendLine($"- Vector Store Query ({extractedDetails.VectorStore} - use specific type from hint, mode: query - this will be part of the Vector Tool). Credential ID: {vectorStoreCredentialId}");
        promptBuilder.AppendLine("- Vector Tool (@n8n/n8n-nodes-langchain.toolVectorStore)");
        promptBuilder.AppendLine("- Window Memory (@n8n/n8n-nodes-langchain.memoryBufferWindow)");
        promptBuilder.AppendLine($"- Chat Model ({configuredLlmProvider} - use specific type like @n8n/n8n-nodes-langchain.lmChatOpenAi or @n8n/n8n-nodes-langchain.lmChatOllama from hint). Credential ID: {chatModelCredentialId}");
        promptBuilder.AppendLine("- RAG Agent (@n8n/n8n-nodes-langchain.agent)");
        promptBuilder.AppendLine("- Google Sheets for logging (n8n-nodes-base.googleSheets). Credential ID: GOOGLE_SHEETS_CREDENTIALS_ID");
        promptBuilder.AppendLine("- Slack for error handling (n8n-nodes-base.slack). Credential ID: SLACK_CREDENTIALS_ID");
        promptBuilder.AppendLine("\nReturn ONLY the JSON object as a single block of text, without any surrounding explanations or markdown formatting.");

        return promptBuilder.ToString();
    }

    public async Task<string> GenerateWorkflowJsonAsync(string userFullDescription)
    {
        var extractedDetails = UserInputParser.ExtractDetails(userFullDescription);
        string prompt = ConstructLlmPrompt(userFullDescription, extractedDetails);

        Console.WriteLine("\n--- Constructed LLM Prompt (C#) ---");
        // Console.WriteLine(prompt); // Full prompt can be very long
        Console.WriteLine($"Prompt for '{extractedDetails.WorkflowName}' constructed. Length: {prompt.Length} chars.");
        Console.WriteLine($"User Goal: {userFullDescription}");
        Console.WriteLine($"LLM Provider for Chat: {_appSettings.LLM?.Provider}, Model: {_appSettings.LLM?.Ollama?.Model ?? _appSettings.LLM?.Gemini?.Model}");
        Console.WriteLine($"Embedding Service: {extractedDetails.EmbeddingService}, Vector Store: {extractedDetails.VectorStore}");
        Console.WriteLine("--- End of LLM Prompt Snippet (C#) ---\n");

        string llmResponse = await _llmService.GenerateWorkflowAsync(prompt);

        // Optional: Basic cleaning if LLM wraps output in markdown code blocks
        if (llmResponse.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            llmResponse = llmResponse.Substring(7);
            if (llmResponse.EndsWith("```"))
            {
                llmResponse = llmResponse.Substring(0, llmResponse.Length - 3);
            }
        }
        else if (llmResponse.StartsWith("```")) // Catch ``` only case
        {
            llmResponse = llmResponse.Substring(3);
             if (llmResponse.EndsWith("```"))
            {
                llmResponse = llmResponse.Substring(0, llmResponse.Length - 3);
            }
        }
        llmResponse = llmResponse.Trim();

        return llmResponse;
    }
}
