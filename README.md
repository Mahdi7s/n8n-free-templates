# n8n-free-templates
# 🚀 200 Ready-to-Import n8n Workflows  

_AI • Vector DB • LLM • DevOps • Finance • IoT • and more_

Free n8n templates to download

!!! Some of the templates are incomplete, you can be a contributor by completing it.
<p align="center">
  <img src="https://img.shields.io/badge/Templates-200-6A5ACD?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Categories-20%2B-008080?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Tech_Mix-Pinecone%2C_Weaviate%2C_Supabase%2C_Redis%2C_OpenAI%2C_Claude%2C_Cohere-FF69B4?style=for-the-badge" />
</p>

> **TL;DR** – Import any JSON workflow below into n8n and hit **Activate**.  
> Each one ships with docs, guard‑rails, error alerts, and (when helpful) a full **RAG** stack.

---

## 🗺️ Categories & Counts

| Category | # Templates |
|----------|-------------|
| AI & Machine Learning | 10 |
| Email Automation | 10 |
| Social Media | 10 |
| Finance & Accounting | 10 |
| E‑Commerce & Retail | 10 |
| Data Collection & Analytics | 10 |
| Education | 5 |
| HR & Recruitment | 5 |
| Healthcare & Wellness | 5 |
| DevOps & Development | 5 |
| Productivity | 10 |
| Government & NGO | 5 |
| Creative & Content | 5 |
| Real Estate | 10 |
| Legal‑Tech | 10 |
| Gaming | 10 |
| Travel | 10 |
| Energy | 10 |
| Manufacturing | 10 |
| Agriculture | 10 |
| Media | 10 |
| IoT | 10 |
| Automotive | 10 |

**Total = 200 JSON workflows**

---

## 🔧 Tech Stack Matrix

| Layer | Options Used |
|-------|--------------|
| **Vector Stores** | Pinecone • Weaviate • Supabase Vector • Redis |
| **Embeddings** | OpenAI • Cohere • Hugging Face |
| **LLM Chat** | OpenAI GPT‑4(o) • Anthropic Claude 3 • Hugging Face Inference |
| **Memory** | Zep Memory • Window Buffer |
| **Extras** | Slack alerts • Google Sheets logs • OCR • HTTP polling |

---

## 📂 Folder Layout

```
<category>/
  *.json         # workflow files
  README.md      # tech mix per file

MASTER_README.md # ← you are here
```

---

## 🚀 Quick Start

```bash
git clone <https://github.com/wassupjay/n8n-free-templates.git>
# then in n8n:
# Settings ▸ Import Workflows ▸ select any JSON
# Open each node ▸ Credentials ▸ choose or create your account
# Save & Activate ✅
```

---

## 🤖 N8n Workflow Generator (Experimental)

This directory also includes an experimental Python-based command-line tool to generate basic n8n workflow JSON, primarily focusing on the common Retrieval Augmented Generation (RAG) pattern observed in many of the templates.

**How it Works (Conceptual):**

1.  The user provides a textual description of the desired n8n agent functionality via the command line.
2.  The tool's `workflow_generator.py` script parses this description for simple keywords (e.g., preferred services) and constructs a detailed prompt.
3.  Currently, instead of sending this prompt to a live Large Language Model (LLM), it uses this prompt to populate a **hardcoded n8n workflow template** that mimics what an LLM might generate. This allows for testing the structure and validation.
4.  The generated JSON is then validated by `validator.py` for basic structural integrity and adherence to n8n conventions.
5.  The user is shown the (simulated) generated JSON and can save it to a file.

**Requirements:**

*   Python 3.x

**How to Run:**

1.  Navigate to the root directory of this repository in your terminal.
2.  Run the application using:
    ```bash
    python app.py
    ```

**Example Usage:**

1.  Run the script: `python app.py`
2.  When prompted, enter a description for your workflow. For example:
    ```
    Please describe the n8n agent functionality you want to create: Create an agent to analyze customer feedback received via a webhook. It should use OpenAI for embeddings and chat, Supabase for the vector store, and log results to Google Sheets. The agent should be named Customer Feedback Analyzer.
    ```
3.  The script will then output the (currently template-based) generated n8n JSON. If valid, it will look something like this (output from `workflow_generator.py` is printed first, then the validated JSON):

    ```
    Generated LLM Prompt (Sample):
    You are an expert n8n workflow generator... [rest of the generated prompt] ...

    Successfully generated and validated the workflow!
    {
        "name": "Customer Feedback Analyzer",
        "nodes": [
            // ... numerous nodes for webhook, text splitter, embeddings, vector store, agent, etc. ...
            // ... (details depend on the hardcoded template in workflow_generator.py) ...
        ],
        "connections": {
            // ... connections between all the nodes ...
        },
        "settings": {
            "executionOrder": "v1"
        },
        "triggerCount": 1
    }
    Save workflow to a file? (y/n): y
    Enter filename (default: customer-feedback-analyzer.json): customer-feedback-analyzer.json
    Workflow saved to customer-feedback-analyzer.json
    ```

**Current Limitations & Future Scope:**

*   **LLM Simulation:** The core generation logic currently uses a hardcoded template string within `workflow_generator.py` that is populated based on the extracted details. A future step would be to integrate a real LLM.
*   **Basic Prompt Parsing:** User prompt parsing in `extract_details_from_prompt` is very basic and keyword-driven. More sophisticated NLP would be needed for nuanced requests.
*   **Limited Customization:** The generation is heavily tied to the "Standard RAG Pattern."
*   **Basic Validation:** The `validator.py` performs essential structural checks but is not a complete n8n validator.

This tool provides a foundational framework for exploring programmatic n8n workflow generation.

## 🤝 Contributing
You can contribute by adding more templates!
!!! Some of the templates are incomplete, you can be a contributor by completing it!!!
PRs & issues welcome!  
Request new templates, improve prompts, or add integrations.
Collabs welcome!
---

_Made with ❤️ by Jay — “Automate everything, then automate the automation.”_
