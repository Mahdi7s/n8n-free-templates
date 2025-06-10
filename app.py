import json
from workflow_generator import generate_workflow_json_string, slugify
from validator import validate_workflow

def main():
    """
    Main function to run the AI Agent to n8n Workflow generator.
    It prompts the user for a description, generates a workflow JSON,
    validates it, and optionally saves it to a file.
    """
    print("Welcome to the AI Agent to n8n Workflow Generator!")
    user_description = input("Please describe the n8n agent functionality you want to create: ")

    if not user_description.strip():
        print("No description provided. Exiting.")
        return

    print("\\nGenerating workflow JSON...")
    generated_json_string = generate_workflow_json_string(user_description)

    print("\\nValidating generated workflow...")
    is_valid, errors_or_messages = validate_workflow(generated_json_string)

    if is_valid:
        print("\\nSuccessfully generated and validated the workflow!")

        # Pretty print the JSON
        try:
            parsed_json = json.loads(generated_json_string)
            print("Generated Workflow JSON:")
            print(json.dumps(parsed_json, indent=2))

            save_to_file = input("\\nSave workflow to a file? (y/n): ").strip().lower()
            if save_to_file == 'y':
                workflow_name = parsed_json.get("name", "my-ai-agent")
                default_filename = f"{slugify(workflow_name)}.json"
                filename_prompt = f"Enter filename (default: {default_filename}): "
                filename = input(filename_prompt).strip() or default_filename

                try:
                    with open(filename, 'w') as f:
                        f.write(generated_json_string)
                    print(f"Workflow saved to '{filename}'")
                except IOError as e:
                    print(f"Error saving file: {e}")

        except json.JSONDecodeError:
            # This case should ideally not be reached if validate_workflow works correctly
            # and is_valid is True, but as a fallback.
            print("\\nError: Could not parse the generated JSON for pretty printing or saving.")
            print("Raw generated string was:")
            print(generated_json_string)

    else:
        print("\\nWorkflow generation failed validation.")
        print("Errors found:")
        if errors_or_messages: # Should be a list of error strings
            for error in errors_or_messages:
                print(f"- {error}")
        else:
            # This case should not happen if is_valid is False,
            # as errors_or_messages should contain at least one message.
            print("- Unknown validation error.")

        print("\\n--- Faulty Generated JSON String ---")
        print(generated_json_string)
        print("--- End of Faulty String ---")


if __name__ == '__main__':
    main()
