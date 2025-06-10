// N8nWorkflowGenerator/ILlmService.cs
using System.Threading.Tasks;

namespace N8nWorkflowGenerator;

public interface ILlmService
{
    Task<string> GenerateWorkflowAsync(string prompt);
}
