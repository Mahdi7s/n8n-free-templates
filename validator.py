import json

def is_valid_json(json_string: str) -> tuple[bool, dict | None, str | None]:
    """
    Tries to parse the json_string.
    Returns (True, parsed_json, None) on success.
    Returns (False, None, "Invalid JSON format: <error message>") on failure.
    """
    try:
        parsed_json = json.loads(json_string)
        return True, parsed_json, None
    except json.JSONDecodeError as e:
        return False, None, f"Invalid JSON format: {str(e)}"

def validate_n8n_structure(n8n_json: dict) -> tuple[bool, list[str]]:
    """
    Validates the basic structure of an n8n workflow JSON.
    Takes a Python dictionary (parsed JSON).
    Returns (True, []) if valid, or (False, [list of error messages]) if invalid.
    """
    errors = []

    # Top-Level Key Checks
    required_top_level_keys = {
        "name": str,
        "nodes": list,
        "connections": dict,
        "settings": dict,
        "triggerCount": int,
        # "versionId": str # Optional, but good to have
    }

    for key, expected_type in required_top_level_keys.items():
        if key not in n8n_json:
            errors.append(f"Missing top-level key: '{key}'")
        elif not isinstance(n8n_json[key], expected_type):
            errors.append(f"Top-level key '{key}' has incorrect type. Expected {expected_type.__name__}, got {type(n8n_json[key]).__name__}")

    # Node-Level Checks
    if "nodes" in n8n_json and isinstance(n8n_json["nodes"], list):
        node_ids = set()
        for i, node in enumerate(n8n_json["nodes"]):
            if not isinstance(node, dict):
                errors.append(f"Node at index {i} is not a dictionary.")
                continue

            node_errors_prefix = f"Node '{node.get('name', 'Unnamed Node with ID ' + str(node.get('id', 'Unknown ID')))}' (index {i})"

            required_node_keys = {
                "id": str,
                "name": str,
                "type": str,
                "typeVersion": (int, float), # Can be int or float (e.g., 1 or 1.0)
                "parameters": dict,
                # "position": list # Optional, but good to have
            }
            for key, expected_type in required_node_keys.items():
                if key not in node:
                    errors.append(f"{node_errors_prefix}: Missing key: '{key}'")
                elif not isinstance(node[key], expected_type):
                    type_names = expected_type if isinstance(expected_type, tuple) else (expected_type,)
                    type_names_str = ", ".join([t.__name__ for t in type_names])
                    errors.append(f"{node_errors_prefix}: Key '{key}' has incorrect type. Expected {type_names_str}, got {type(node[key]).__name__}")

            if "id" in node and isinstance(node["id"], str):
                if node["id"] in node_ids:
                    errors.append(f"{node_errors_prefix}: Duplicate node ID: '{node['id']}'")
                else:
                    node_ids.add(node["id"])

    # Connection-Level Checks
    if "connections" in n8n_json and isinstance(n8n_json["connections"], dict):
        for source_node_name, connection_details in n8n_json["connections"].items():
            # source_node_name should correspond to a node name or id, but we are not checking that link here for simplicity.
            # It's also often the node's 'name' field, not its 'id'. n8n can be a bit flexible here.
            if not isinstance(connection_details, dict):
                errors.append(f"Connection details for source '{source_node_name}' are not a dictionary.")
                continue

            for conn_type, targets in connection_details.items(): # e.g., "main", "ai_tool"
                if not isinstance(targets, list):
                    errors.append(f"Targets for source '{source_node_name}', connection type '{conn_type}' are not a list.")
                    continue
                for i, target_list in enumerate(targets):
                    if not isinstance(target_list, list):
                        errors.append(f"Target entry at index {i} for source '{source_node_name}', connection type '{conn_type}' is not a list.")
                        continue
                    for j, target_obj in enumerate(target_list):
                        if not isinstance(target_obj, dict):
                            errors.append(f"Target object at index [{i}][{j}] for source '{source_node_name}', connection type '{conn_type}' is not a dictionary.")
                            continue
                        if "node" not in target_obj or not isinstance(target_obj["node"], str):
                            errors.append(f"Target object at index [{i}][{j}] for source '{source_node_name}', connection type '{conn_type}' is missing 'node' key or it's not a string.")
                        if "type" not in target_obj or not isinstance(target_obj["type"], str):
                             errors.append(f"Target object at index [{i}][{j}] for source '{source_node_name}', connection type '{conn_type}' is missing 'type' key or it's not a string.")

    if not errors:
        return True, []
    else:
        return False, errors

def validate_workflow(json_string: str) -> tuple[bool, list[str]]:
    """
    Main validation function. First checks for valid JSON, then n8n structure.
    Returns (True, []) if valid, or (False, [list of error messages]) if invalid.
    """
    is_valid, parsed_data, error_message = is_valid_json(json_string)

    if not is_valid:
        return False, [error_message]

    # Type assertion for linters/type checkers, as is_valid_json ensures parsed_data is dict if is_valid is True
    assert parsed_data is not None
    return validate_n8n_structure(parsed_data)


if __name__ == '__main__':
    # Example of a valid (minimal) n8n JSON string
    # Taken from workflow_generator.py's hardcoded output (simplified)
    valid_n8n_json_string = """
    {
      "name": "My AI Agent",
      "nodes": [
        {
          "parameters": {
            "httpMethod": "POST",
            "path": "my-ai-agent-webhook",
            "options": {}
          },
          "id": "e8f9b0a1-2c3d-4e5f-8a7b-6c5d4e3f2a1b",
          "name": "Webhook Trigger",
          "type": "n8n-nodes-base.webhook",
          "typeVersion": 1,
          "position": [-300, 0]
        }
      ],
      "connections": {
        "Webhook Trigger": {
          "main": [
            [
              {
                "node": "Some Other Node",
                "type": "main",
                "index": 0
              }
            ]
          ]
        }
      },
      "settings": { "executionOrder": "v1" },
      "triggerCount": 1,
      "versionId": "0f1a2b3c-4d5e-6f7a-8b9c-0d1e2f3a4b5c"
    }
    """

    print("--- Testing VALID n8n JSON ---")
    is_ok, errors = validate_workflow(valid_n8n_json_string)
    print(f"Validation Result: {'OK' if is_ok else 'Failed'}")
    if errors:
        for err in errors:
            print(f"- {err}")
    print("\\n")

    # Example of an invalid JSON string
    invalid_json_string = """
    {
      "name": "Test",
      "nodes": [
        {"id": "1", "name": "Node1", "type": "typeA", "typeVersion": 1, "parameters": {}} # Missing comma
        {"id": "2", "name": "Node2", "type": "typeB", "typeVersion": 1, "parameters": {}}
      ]
    }
    """
    print("--- Testing INVALID JSON string ---")
    is_ok, errors = validate_workflow(invalid_json_string)
    print(f"Validation Result: {'OK' if is_ok else 'Failed'}")
    if errors:
        for err in errors:
            print(f"- {err}")
    print("\\n")

    # Example of valid JSON but invalid n8n structure
    invalid_n8n_structure_json_string = """
    {
      "workflowName": "My Bad Workflow",
      "nodes_list": [],
      "links": {},
      "preferences": {},
      "triggers": 0
    }
    """
    print("--- Testing VALID JSON but INVALID n8n structure ---")
    is_ok, errors = validate_workflow(invalid_n8n_structure_json_string)
    print(f"Validation Result: {'OK' if is_ok else 'Failed'}")
    if errors:
        for err in errors:
            print(f"- {err}")
    print("\\n")

    # Example with more specific n8n structure errors
    more_invalid_n8n_structure_json_string = """
    {
      "name": "More Errors",
      "nodes": [
        {
          "id": "node1",
          "name": "Webhook",
          "type": "n8n-nodes-base.webhook",
          "typeVersion": "1.0",
          "parameters": {}
        },
        {
          "id": "node1",
          "name": "Duplicate ID Node",
          "type": "n8n-nodes-base.set",
          "typeVersion": 1,
          "parameters": "not a dict"
        }
      ],
      "connections": {
        "Webhook": {
            "main": [
                "not a list of lists"
            ]
        }
      },
      "settings": {},
      "triggerCount": "1"
    }
    """
    print("--- Testing MORE specific n8n structure errors ---")
    is_ok, errors = validate_workflow(more_invalid_n8n_structure_json_string)
    print(f"Validation Result: {'OK' if is_ok else 'Failed'}")
    if errors:
        for err in errors:
            print(f"- {err}")
    print("\\n")

    # Example with missing node ID
    missing_node_id_json_string = """
    {
      "name": "Missing Node ID",
      "nodes": [
        {
          "name": "Webhook",
          "type": "n8n-nodes-base.webhook",
          "typeVersion": 1,
          "parameters": {}
        }
      ],
      "connections": {},
      "settings": { "executionOrder": "v1" },
      "triggerCount": 1,
      "versionId": "0f1a2b3c-4d5e-6f7a-8b9c-0d1e2f3a4b5c"
    }
    """
    print("--- Testing missing node ID ---")
    is_ok, errors = validate_workflow(missing_node_id_json_string)
    print(f"Validation Result: {'OK' if is_ok else 'Failed'}")
    if errors:
      for err in errors:
        print(f"- {err}")
    print("\\n")
