using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services;

/// <summary>
/// Represents a tool/function that can be called by the AI
/// </summary>
public interface IToolService
{
    /// <summary>
    /// The name of the tool (e.g., "search_products")
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Description of what the tool does
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// JSON schema for the tool's parameters
    /// </summary>
    ToolParameterSchema Parameters { get; }
    
    /// <summary>
    /// Executes the tool with the given parameters
    /// </summary>
    Task<ToolExecutionResult> ExecuteAsync(ToolCall toolCall, CancellationToken cancellationToken = default);
}

