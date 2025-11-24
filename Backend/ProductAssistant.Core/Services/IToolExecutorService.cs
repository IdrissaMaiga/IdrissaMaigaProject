using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services;

/// <summary>
/// Interface for tool executor service
/// </summary>
public interface IToolExecutorService
{
    Task<ToolExecutionResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default);
    List<GeminiTool> GetAvailableTools();
    bool HasTool(string toolName);
}

