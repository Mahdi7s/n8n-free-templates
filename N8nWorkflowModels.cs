// N8nWorkflowGenerator/N8nWorkflowModels.cs
using System.Collections.Generic;
using System.Text.Json.Serialization; // Required for JsonPropertyName

namespace N8nWorkflowGenerator;

public class N8nWorkflow
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("nodes")]
    public List<N8nNode> Nodes { get; set; } = new();

    [JsonPropertyName("connections")]
    public Dictionary<string, NodeConnectionDetails> Connections { get; set; } = new(); // Key is source node name

    [JsonPropertyName("settings")]
    public WorkflowSettings Settings { get; set; } = new();

    [JsonPropertyName("triggerCount")]
    public int TriggerCount { get; set; } = 0; // Typically 1 for webhook triggered workflows

    [JsonPropertyName("versionId")] // Added as it's often in n8n JSON and useful
    public string VersionId { get; set; } = Helpers.GenerateUuid();
}

public class N8nNode
{
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = new(); // Can be complex, using object for flexibility

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("typeVersion")]
    public int TypeVersion { get; set; } // n8n uses int or float, stick to int for simplicity or use double if needed

    [JsonPropertyName("position")]
    public List<int> Position { get; set; } = new(); // e.g., [x, y]

    [JsonPropertyName("credentials")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] // Don't serialize if null
    public Dictionary<string, CredentialEntry>? Credentials { get; set; }
}

public class CredentialEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

public class NodeConnectionDetails
{
    // Represents connections like "main", "ai_tool", "onError"
    // Each key (e.g., "main") holds a list of target connection arrays
    [JsonExtensionData] // Allows dynamic properties like "main", "ai_tool"
    public Dictionary<string, List<List<ConnectionTarget>>> ConnectionTypes { get; set; } = new();
}

public class ConnectionTarget
{
    [JsonPropertyName("node")]
    public string Node { get; set; } = string.Empty; // Target node name (should be ID of target)

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // Type of the input on the target node (e.g., "main")

    [JsonPropertyName("index")]
    public int Index { get; set; } = 0; // Typically 0
}

public class WorkflowSettings
{
    [JsonPropertyName("executionOrder")]
    public string ExecutionOrder { get; set; } = "v1";
}
