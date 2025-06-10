// N8nWorkflowGenerator/UserInputParser.cs
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace N8nWorkflowGenerator;

public class WorkflowDetails
{
    public string WorkflowName { get; set; } = "My AI Agent";
    public string EmbeddingService { get; set; } = "OpenAI"; // Default
    public string VectorStore { get; set; } = "Supabase";    // Default
    public string ChatModelService { get; set; } = "OpenAI"; // Default
    // Add other details as needed
}

public static class UserInputParser
{
    public static WorkflowDetails ExtractDetails(string userPrompt)
    {
        var details = new WorkflowDetails();

        // Workflow Name Extraction
        // Regex updated to be more flexible with quotes or no quotes
        var nameMatch = Regex.Match(userPrompt, @"(?:named|called)\s+(?:['""]?)([\w\s]+?)(?:['""]?)(?:\s|$|,|\.)", RegexOptions.IgnoreCase);
        if (nameMatch.Success && !string.IsNullOrWhiteSpace(nameMatch.Groups[1].Value))
        {
            details.WorkflowName = nameMatch.Groups[1].Value.Trim();
        }
        else
        {
            // Basic fallback: if the prompt starts with something that looks like a title
            // e.g. "Create an agent..." -> "Create An Agent"
            // This is very naive, a better approach would be needed for robust extraction
            var words = userPrompt.Split(' ');
            if (words.Length > 2 && words[0].ToLowerInvariant() == "create" && (words[1].ToLowerInvariant() == "an" || words[1].ToLowerInvariant() == "a"))
            {
                // Attempt to find a plausible name, could be improved
                var potentialName = new List<string>();
                for(int i = 2; i < words.Length; i++)
                {
                    if (char.IsUpper(words[i][0]) || (potentialName.Count > 0 && words[i].ToLowerInvariant() != "using" && words[i].ToLowerInvariant() != "with"))
                    {
                        potentialName.Add(words[i]);
                    }
                    else
                    {
                        break;
                    }
                }
                if(potentialName.Count > 0)
                {
                    details.WorkflowName = string.Join(" ", potentialName);
                }
            }
        }

        // Embedding Service
        if (Regex.IsMatch(userPrompt, @"Cohere embeddings", RegexOptions.IgnoreCase))
            details.EmbeddingService = "Cohere";
        else if (Regex.IsMatch(userPrompt, @"HuggingFace embeddings", RegexOptions.IgnoreCase))
            details.EmbeddingService = "HuggingFace";
        else if (Regex.IsMatch(userPrompt, @"OpenAI embeddings", RegexOptions.IgnoreCase)) // Ensure this doesn't override a more specific LLM provider choice later
            details.EmbeddingService = "OpenAI";
        // else if (Program.AppConfiguration?.LLM?.Provider.Equals("Gemini", System.StringComparison.OrdinalIgnoreCase))
        // details.EmbeddingService = "Gemini"; // Or a specific Gemini embedding model name

        // Vector Store
        if (Regex.IsMatch(userPrompt, @"Pinecone", RegexOptions.IgnoreCase))
            details.VectorStore = "Pinecone";
        else if (Regex.IsMatch(userPrompt, @"Weaviate", RegexOptions.IgnoreCase))
            details.VectorStore = "Weaviate";
        else if (Regex.IsMatch(userPrompt, @"Redis", RegexOptions.IgnoreCase))
            details.VectorStore = "Redis";
        else if (Regex.IsMatch(userPrompt, @"Supabase", RegexOptions.IgnoreCase))
            details.VectorStore = "Supabase";

        // Chat Model Service
        if (Regex.IsMatch(userPrompt, @"Anthropic chat", RegexOptions.IgnoreCase))
            details.ChatModelService = "Anthropic";
        else if (Regex.IsMatch(userPrompt, @"HuggingFace chat", RegexOptions.IgnoreCase))
            details.ChatModelService = "HuggingFace";
        else if (Regex.IsMatch(userPrompt, @"OpenAI chat", RegexOptions.IgnoreCase)) // Ensure this doesn't override a more specific LLM provider choice
            details.ChatModelService = "OpenAI";
        // else if (Program.AppConfiguration?.LLM?.Provider.Equals("Gemini", System.StringComparison.OrdinalIgnoreCase))
        // details.ChatModelService = "Gemini";


        // Potentially align ChatModelService and EmbeddingService with AppConfiguration.LLM.Provider if not specified
        // This logic can be complex: user might specify OpenAI embeddings but use Ollama for chat with a non-OpenAI model.
        // For now, explicit mentions in prompt take precedence.
        // If AppSettings.LLM.Provider is "Gemini" and no chat/embedding explicitly set to something else,
        // it might be reasonable to default details.ChatModelService and details.EmbeddingService to "Gemini".
        // This requires access to AppConfiguration here, or passing it in.
        // Example (needs Program.AppConfiguration to be accessible, or pass AppSettings):
        /*
        var llmProvider = Program.AppConfiguration?.LLM?.Provider;
        if (!string.IsNullOrEmpty(llmProvider)) {
            if (details.EmbeddingService == "OpenAI" && llmProvider.Equals("Gemini", System.StringComparison.OrdinalIgnoreCase)) {
                // If user didn't explicitly ask for OpenAI embeddings, and provider is Gemini, assume Gemini for embeddings.
                // This heuristic might need refinement.
            }
            if (details.ChatModelService == "OpenAI" && llmProvider.Equals("Gemini", System.StringComparison.OrdinalIgnoreCase)) {
                 // Similar logic for chat model
            }
        }
        */

        return details;
    }
}
