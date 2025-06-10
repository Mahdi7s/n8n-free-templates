import uuid
import re

def generate_uuid() -> str:
    """Generates a new UUID string."""
    return str(uuid.uuid4())

def slugify(name: str) -> str:
    """
    Slugifies a string by converting to lowercase, replacing spaces and
    special characters with hyphens, and removing duplicate hyphens.
    """
    name = name.lower()
    name = re.sub(r'[^a-z0-9\s-]', '', name)  # Remove special characters except hyphens and spaces
    name = re.sub(r'\s+', '-', name)          # Replace spaces with hyphens
    name = re.sub(r'-+', '-', name)          # Remove duplicate hyphens
    name = name.strip('-')                   # Remove leading/trailing hyphens
    return name

def extract_details_from_prompt(user_prompt: str) -> dict:
    """
    Extracts basic details from the user prompt using keyword spotting.
    """
    details = {
        'workflow_name': "My AI Agent",
        'embedding_service': "OpenAI",
        'vector_store': "Supabase",
        'chat_model_service': "OpenAI",
    }

    # Workflow name extraction
    name_match = re.search(r"(?:named|called)\s*['\"]([^'\"]+)['\"]", user_prompt, re.IGNORECASE)
    if name_match:
        details['workflow_name'] = name_match.group(1)
    else:
        # Basic noun phrase extraction (very simplified)
        # This is a placeholder for more sophisticated NLP
        words = [word for word in user_prompt.split() if word.isalnum()]
        if len(words) >= 2 and words[0].istitle(): # Simple heuristic: "My Workflow"
             potential_name = " ".join(w for w in words[:3] if w.istitle() or w.islower())
             if potential_name:
                details['workflow_name'] = potential_name


    # Embedding service
    if "cohere embeddings" in user_prompt.lower():
        details['embedding_service'] = "Cohere"
    elif "huggingface embeddings" in user_prompt.lower():
        details['embedding_service'] = "HuggingFace"
    elif "openai embeddings" in user_prompt.lower(): # Explicitly check for OpenAI too
        details['embedding_service'] = "OpenAI"


    # Vector store
    if "pinecone" in user_prompt.lower():
        details['vector_store'] = "Pinecone"
    elif "weaviate" in user_prompt.lower():
        details['vector_store'] = "Weaviate"
    elif "redis" in user_prompt.lower():
        details['vector_store'] = "Redis"
    elif "supabase" in user_prompt.lower(): # Explicitly check for Supabase
        details['vector_store'] = "Supabase"


    # Chat model service
    if "anthropic chat" in user_prompt.lower():
        details['chat_model_service'] = "Anthropic"
    elif "huggingface chat" in user_prompt.lower():
        details['chat_model_service'] = "HuggingFace"
    elif "openai chat" in user_prompt.lower(): # Explicitly check for OpenAI
        details['chat_model_service'] = "OpenAI"

    return details

def construct_llm_prompt(user_description: str, details: dict) -> str:
    """
    Constructs the LLM prompt based on user description and extracted details.
    """
    workflow_name = details.get('workflow_name', 'My AI Agent')
    webhook_path = slugify(workflow_name) + "-webhook"
    index_name = slugify(workflow_name) + "-index"
    # Placeholder for knowledge base path, assuming it's derived or a standard name
    knowledge_base_path = slugify(workflow_name) + "-knowledge-base"


    prompt = f"""
You are an expert n8n workflow developer. Your task is to generate a valid n8n workflow JSON based on the user's requirements.

User requirement: "{user_description}"

Workflow Details:
- Workflow Name: "{workflow_name}"
- Webhook Path: "{webhook_path}" (derived from workflow name, ensure it's URL-friendly)
- Embedding Service: "{details.get('embedding_service')}"
- Vector Store: "{details.get('vector_store')}"
- Chat Model Service: "{details.get('chat_model_service')}"
- Index Name for Vector Store: "{index_name}" (derived from workflow name)
- Knowledge Base Path (if applicable, for file-based sources): "{knowledge_base_path}"

Instructions for the n8n JSON structure:

1.  **Webhook Trigger:**
    *   Start with a Webhook node (`n8n-nodes-base.webhook`).
    *   Set `httpMethod` to "POST".
    *   Set `path` to "{webhook_path}".
    *   Generate a unique `id` for this node.

2.  **Embedding Node:**
    *   The type of this node will depend on the chosen `embedding_service`.
        *   OpenAI: `n8n-nodes-langchain.llmOpenAiEmbeddings`
        *   Cohere: `n8n-nodes-langchain.llmCohereEmbeddings`
        *   HuggingFace: `n8n-nodes-langchain.llmHuggingFaceEmbeddings`
    *   Connect the Webhook output to this node's input.
    *   The text to embed will typically come from the Webhook body (e.g., `{{$json.body.question}}` or similar).
    *   Generate a unique `id` for this node.

3.  **Vector Store Node (Upsert/Query):**
    *   The type of this node will depend on the chosen `vector_store`.
        *   Supabase: `n8n-nodes-langchain.vectorStoreSupabase` (ensure `operation` is set to "upsert" or "query" as appropriate)
        *   Pinecone: `n8n-nodes-langchain.vectorStorePinecone`
        *   Weaviate: `n8n-nodes-langchain.vectorStoreWeaviate`
        *   Redis: `n8n-nodes-langchain.vectorStoreRedis`
    *   This node will take the embeddings from the previous step.
    *   It will also need configuration for the specific vector store (API keys, URLs, `indexName` which should be "{index_name}").
    *   For a RAG agent, this node will likely perform a "query" operation to find relevant documents.
    *   Generate a unique `id` for this node.

4.  **Chat Model Node (LLM for Response Generation):**
    *   The type of this node will depend on the chosen `chat_model_service`.
        *   OpenAI: `n8n-nodes-langchain.chatOpenAi`
        *   Anthropic: `n8n-nodes-langchain.chatAnthropic`
        *   HuggingFace: `n8n-nodes-langchain.chatHuggingFace`
    *   This node will take the retrieved documents from the vector store and the original query.
    *   It will be prompted to generate a response based on the context and query.
    *   Generate a unique `id` for this node.

5.  **Respond to Webhook Node:**
    *   Use an `n8n-nodes-base.respondToWebhook` node.
    *   Connect the Chat Model's output to this node.
    *   This node will send the generated response back to the user.
    *   Generate a unique `id` for this node.

Ensure all node `id`s are unique strings (UUIDs are recommended).
Ensure `connections` are correctly defined between nodes.
Set `settings.executionOrder` to "v1".
Set `triggerCount` to 1 if a Webhook is the only trigger.

Please generate the complete n8n JSON now.
"""
    return prompt

def generate_workflow_json_string(user_description: str) -> str:
    """
    Main function to generate a workflow JSON string.
    Currently simulates LLM call and returns a hardcoded JSON.
    """
    details = extract_details_from_prompt(user_description)
    llm_prompt = construct_llm_prompt(user_description, details)

    print("----------- CONSTRUCTED LLM PROMPT -----------")
    print(llm_prompt)
    print("----------------------------------------------")

    # Simulate LLM response with a hardcoded, minimal n8n JSON string
    # This will be replaced by an actual LLM call in the future.
    # Placeholders are used where the LLM would fill in details.

    workflow_name = details.get('workflow_name', "My AI Agent")
    webhook_path = slugify(workflow_name) + "-webhook"
    # Acknowledge other placeholders that an LLM would use based on the prompt
    # For this initial step, we'll keep the JSON very simple as per instructions.

    # Using the simple example from the prompt for now, will expand later
    # to be more representative of RAG and use more placeholders.
    return f'''{{
      "name": "{workflow_name}",
      "nodes": [
        {{
          "parameters": {{
            "httpMethod": "POST",
            "path": "{webhook_path}",
            "options": {{}}
          }},
          "id": "{generate_uuid()}",
          "name": "Webhook Trigger",
          "type": "n8n-nodes-base.webhook",
          "typeVersion": 1,
          "position": [-300, 0]
        }},
        {{
          "parameters": {{
            "model": "text-embedding-ada-002",
            "text": "{{{{$json.body.question}}}}",
            "options": {{}}
          }},
          "id": "{generate_uuid()}",
          "name": "EMBEDDING_NODE_TYPE_PLACEHOLDER",
          "type": "n8n-nodes-langchain.llmOpenAiEmbeddings",
          "typeVersion": 1,
          "position": [-100, 0],
          "credentials": {{
            "openAiApi": {{
              "id": "OPENAI_CREDENTIALS_ID_PLACEHOLDER",
              "name": "OpenAI Credentials"
            }}
          }}
        }},
        {{
          "parameters": {{
            "operation": "query",
            "indexName": "INDEX_NAME_PLACEHOLDER",
            "content": "{{{{$json.body.question}}}}",
            "embeddings": "{{{{$json.embeddings}}}}",
            "options": {{}}
          }},
          "id": "{generate_uuid()}",
          "name": "VECTOR_STORE_NODE_TYPE_PLACEHOLDER",
          "type": "n8n-nodes-langchain.vectorStoreSupabase",
          "typeVersion": 1,
          "position": [100, 0],
          "credentials": {{
            "supabaseVectorApi": {{
              "id": "SUPABASE_CREDENTIALS_ID_PLACEHOLDER",
              "name": "Supabase Credentials"
            }}
          }}
        }},
        {{
          "parameters": {{
            "model": "gpt-3.5-turbo",
            "prompt": "Context:{{{{$json.documents}}}}\\n\\nQuestion:{{{{$json.body.question}}}}\\n\\nAnswer:",
            "options": {{}}
          }},
          "id": "{generate_uuid()}",
          "name": "CHAT_MODEL_NODE_TYPE_PLACEHOLDER",
          "type": "n8n-nodes-langchain.chatOpenAi",
          "typeVersion": 1,
          "position": [300, 0],
          "credentials": {{
            "openAiApi": {{
              "id": "OPENAI_CREDENTIALS_ID_PLACEHOLDER",
              "name": "OpenAI Credentials"
            }}
          }}
        }},
        {{
          "parameters": {{
            "responseCode": 200,
            "responseData": "{{{{$json.text}}}}",
            "options": {{}}
          }},
          "id": "{generate_uuid()}",
          "name": "Respond to Webhook",
          "type": "n8n-nodes-base.respondToWebhook",
          "typeVersion": 1,
          "position": [500, 0]
        }}
      ],
      "connections": {{
        "Webhook Trigger": {{
          "main": [
            [
              {{
                "node": "EMBEDDING_NODE_TYPE_PLACEHOLDER",
                "type": "main",
                "index": 0
              }}
            ]
          ]
        }},
        "EMBEDDING_NODE_TYPE_PLACEHOLDER": {{
          "main": [
            [
              {{
                "node": "VECTOR_STORE_NODE_TYPE_PLACEHOLDER",
                "type": "main",
                "index": 0
              }}
            ]
          ]
        }},
        "VECTOR_STORE_NODE_TYPE_PLACEHOLDER": {{
          "main": [
            [
              {{
                "node": "CHAT_MODEL_NODE_TYPE_PLACEHOLDER",
                "type": "main",
                "index": 0
              }}
            ]
          ]
        }},
        "CHAT_MODEL_NODE_TYPE_PLACEHOLDER": {{
          "main": [
            [
              {{
                "node": "Respond to Webhook",
                "type": "main",
                "index": 0
              }}
            ]
          ]
        }}
      }},
      "settings": {{
        "executionOrder": "v1"
      }},
      "triggerCount": 1,
      "versionId": "{generate_uuid()}"
    }}'''

if __name__ == '__main__':
    # Example Usage:
    prompt1 = "Create a workflow named 'Customer Support Bot' that uses OpenAI embeddings, Supabase for storage, and OpenAI for chat."
    json_output1 = generate_workflow_json_string(prompt1)
    print("\\n----------- GENERATED WORKFLOW JSON (Example 1) -----------")
    print(json_output1)
    print("-------------------------------------------------------------")

    prompt2 = "I need an agent called 'Document Analyzer' with Cohere embeddings, Pinecone, and Anthropic chat."
    json_output2 = generate_workflow_json_string(prompt2)
    print("\\n----------- GENERATED WORKFLOW JSON (Example 2) -----------")
    print(json_output2)
    print("-------------------------------------------------------------")

    prompt3 = "An AI agent for my website that uses HuggingFace for embeddings and chat, and Weaviate as a vector database. Call it 'Website Helper'."
    json_output3 = generate_workflow_json_string(prompt3)
    print("\\n----------- GENERATED WORKFLOW JSON (Example 3) -----------")
    print(json_output3)
    print("-------------------------------------------------------------")

    prompt4 = "Default agent" # Test default naming and services
    json_output4 = generate_workflow_json_string(prompt4)
    print("\\n----------- GENERATED WORKFLOW JSON (Example 4) -----------")
    print(json_output4)
    print("-------------------------------------------------------------")

    prompt5 = "An agent named 'My Custom Name' using defaults for services."
    json_output5 = generate_workflow_json_string(prompt5)
    print("\\n----------- GENERATED WORKFLOW JSON (Example 5) -----------")
    print(json_output5)
    print("-------------------------------------------------------------")
