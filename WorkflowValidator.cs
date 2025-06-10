// N8nWorkflowGenerator/WorkflowValidator.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace N8nWorkflowGenerator;

public static class WorkflowValidator
{
    public static (bool IsValid, N8nWorkflow? Workflow, List<string> Errors) ValidateWorkflowJson(string jsonString)
    {
        var errors = new List<string>();
        N8nWorkflow? workflow = null;

        // 1. Check for well-formed JSON and deserialize
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };
            workflow = JsonSerializer.Deserialize<N8nWorkflow>(jsonString, options);

            if (workflow == null)
            {
                errors.Add("Deserialized workflow object is null. The JSON might be empty or not represent a workflow.");
                return (false, null, errors);
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"Invalid JSON format: {ex.Message} (Line: {ex.LineNumber ?? 0}, Pos: {ex.BytePositionInLine ?? 0})");
            return (false, null, errors);
        }
        catch (Exception ex) // Catch other potential deserialization errors
        {
            errors.Add($"An unexpected error occurred during JSON deserialization: {ex.Message}");
            return (false, null, errors);
        }

        // 2. Perform structural n8n validation
        if (string.IsNullOrWhiteSpace(workflow.Name))
        {
            errors.Add("Workflow 'name' is missing or empty.");
        }

        if (workflow.Nodes == null || !workflow.Nodes.Any())
        {
            errors.Add("Workflow 'nodes' array is missing, null, or empty.");
        }
        else
        {
            var nodeIds = new HashSet<string>();
            for (int i = 0; i < workflow.Nodes.Count; i++)
            {
                var node = workflow.Nodes[i];
                if (node == null)
                {
                    errors.Add($"Node at index {i} is null.");
                    continue;
                }
                string nodeIdForError = string.IsNullOrWhiteSpace(node.Id) ? $"at index {i}" : node.Id;
                string nodeNameForError = string.IsNullOrWhiteSpace(node.Name) ? $"ID '{nodeIdForError}'" : node.Name;


                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    errors.Add($"Node '{nodeNameForError}' has a missing or empty 'id'.");
                }
                else
                {
                    if (!nodeIds.Add(node.Id))
                    {
                        errors.Add($"Duplicate node 'id' found: {node.Id}.");
                    }
                }
                if (string.IsNullOrWhiteSpace(node.Name))
                {
                    errors.Add($"Node with id '{nodeIdForError}' has a missing or empty 'name'.");
                }
                if (string.IsNullOrWhiteSpace(node.Type))
                {
                    errors.Add($"Node '{nodeNameForError}' has a missing or empty 'type'.");
                }
                if (node.Parameters == null)
                {
                     errors.Add($"Node '{nodeNameForError}' has null 'parameters'. Should be an empty dictionary if no params.");
                }
                 if (node.TypeVersion == 0 && !node.Type.StartsWith("n8n-nodes-base.stickyNote"))
                 {
                    // This check is indicative; n8n actual typeVersion handling might be more nuanced.
                    // Some core nodes might have typeVersion 0 or 1 by default.
                    // For custom/community nodes, it usually starts at 1.
                    // errors.Add($"Node '{nodeNameForError}' has 'typeVersion' set to 0, which might be an issue for some nodes.");
                 }
                 if (node.Position == null || node.Position.Count != 2)
                 {
                    errors.Add($"Node '{nodeNameForError}' has invalid 'position'. Must be an array of two integers.");
                 }
            }
        }

        if (workflow.Connections == null)
        {
            // An empty connections object {} is valid if there are no connections.
            // But if the 'connections' key itself is missing, it's an issue.
            // Deserialization handles this: if 'connections' is missing, workflow.Connections will be null (if not initialized in model constructor).
            // N8nWorkflowModels initializes Connections = new(), so it won't be null unless JSON explicitly sets it to null.
            // If JSON has "connections": null, then this check is valid.
            // If JSON omits "connections" entirely, workflow.Connections would be the new initialized dict.
            // This check is more for "connections": null in the JSON.
             errors.Add("Workflow 'connections' object is explicitly null. Should be an empty object {} if no connections.");
        }
        else
        {
            foreach (var sourceEntry in workflow.Connections)
            {
                if (string.IsNullOrWhiteSpace(sourceEntry.Key)) {
                     errors.Add("Connection source node name (key in connections object) is empty.");
                }
                // sourceEntry.Key should match one of the node names in workflow.Nodes. (More advanced validation)

                if (sourceEntry.Value == null || sourceEntry.Value.ConnectionTypes == null) {
                     errors.Add($"Connection details for source '{sourceEntry.Key}' are null or invalid (ConnectionTypes is null).");
                     continue;
                }
                foreach (var connectionTypeEntry in sourceEntry.Value.ConnectionTypes)
                {
                    if (string.IsNullOrWhiteSpace(connectionTypeEntry.Key)) {
                        errors.Add($"Connection type (e.g., 'main') for source '{sourceEntry.Key}' is empty.");
                    }
                    if (connectionTypeEntry.Value == null) { // An empty list of target lists is valid. e.g. "main": []
                         errors.Add($"Target lists for connection type '{connectionTypeEntry.Key}' from source '{sourceEntry.Key}' is null. Should be an empty list [].");
                        continue;
                    }
                    foreach(var targetList in connectionTypeEntry.Value)
                    {
                        if (targetList == null) { // A target list itself should not be null. e.g. "main": [ null ] is invalid
                             errors.Add($"Target list for connection type '{connectionTypeEntry.Key}' from source '{sourceEntry.Key}' contains a null inner list.");
                             continue;
                        }
                        if (!targetList.Any()) {
                            // "main": [[]] - an empty target list inside the list of lists. This is unusual but perhaps possible.
                            // Depending on strictness, this could be an error.
                        }
                        foreach (var target in targetList)
                        {
                            if (target == null) {
                                errors.Add($"Target object for connection type '{connectionTypeEntry.Key}' from source '{sourceEntry.Key}' is null.");
                                continue;
                            }
                            if (string.IsNullOrWhiteSpace(target.Node)) // This should be a node ID
                            {
                                errors.Add($"Connection target from '{sourceEntry.Key}' (type '{connectionTypeEntry.Key}') has missing 'node' id.");
                            }
                            // else if (!workflow.Nodes.Any(n => n.Id == target.Node)) { // More advanced: check if target.Node (ID) exists
                            //    errors.Add($"Connection target from '{sourceEntry.Key}' (type '{connectionTypeEntry.Key}') points to a non-existent node ID: {target.Node}.");
                            //}
                            if (string.IsNullOrWhiteSpace(target.Type))
                            {
                                errors.Add($"Connection target from '{sourceEntry.Key}' to node '{target.Node}' has missing 'type' (target port name).");
                            }
                        }
                    }
                }
            }
        }

        if (workflow.Settings == null || string.IsNullOrWhiteSpace(workflow.Settings.ExecutionOrder))
        {
            errors.Add("Workflow 'settings' or 'settings.executionOrder' is missing or invalid.");
        }

        // TriggerCount is int, so it will always have a value (default 0 if not in JSON).
        // n8n usually expects it to be 1 for active workflows with a single trigger.
        // if (workflow.TriggerCount <= 0 && workflow.Nodes.Any(n => n.Type.Contains("trigger", StringComparison.OrdinalIgnoreCase))) {
        //    errors.Add("Workflow 'triggerCount' is not positive, but trigger nodes seem to exist.");
        // }


        return (!errors.Any(), workflow, errors);
    }
}
