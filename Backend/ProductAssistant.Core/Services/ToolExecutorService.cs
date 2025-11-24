using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services;

/// <summary>
/// Service that executes tools based on AI tool calls
/// </summary>
public class ToolExecutorService : IToolExecutorService
{
    private readonly Dictionary<string, IToolService> _tools;
    private readonly ILogger<ToolExecutorService> _logger;

    public ToolExecutorService(IEnumerable<IToolService> tools, ILogger<ToolExecutorService> logger)
    {
        _logger = logger;
        _tools = tools.ToDictionary(t => t.Name, t => t);
        _logger.LogInformation("Initialized ToolExecutorService with {Count} tools: {ToolNames}", 
            _tools.Count, string.Join(", ", _tools.Keys));
    }

    public async Task<ToolExecutionResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolCall.Name))
        {
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = "Tool name is required",
                ErrorCode = "INVALID_TOOL_NAME"
            };
        }

        if (!_tools.TryGetValue(toolCall.Name, out var tool))
        {
            _logger.LogWarning("Tool not found: {ToolName}", toolCall.Name);
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = $"Tool '{toolCall.Name}' not found",
                ErrorCode = "TOOL_NOT_FOUND"
            };
        }

        try
        {
            _logger.LogInformation("Executing tool: {ToolName} with arguments: {Arguments}", 
                toolCall.Name, System.Text.Json.JsonSerializer.Serialize(toolCall.Arguments));
            
            var result = await tool.ExecuteAsync(toolCall, cancellationToken);
            
            _logger.LogInformation("Tool execution completed: {ToolName}, Success: {Success}", 
                toolCall.Name, result.Success);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", toolCall.Name);
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }

    public List<GeminiTool> GetAvailableTools()
    {
        return new List<GeminiTool>
        {
            new GeminiTool
            {
                FunctionDeclarations = _tools.Values.Select(tool => new GeminiFunctionDeclaration
                {
                    Name = tool.Name,
                    Description = tool.Description,
                    Parameters = new GeminiFunctionParameters
                    {
                        Type = tool.Parameters.Type,
                        Properties = tool.Parameters.Properties.ToDictionary(
                            kvp => kvp.Key,
                            kvp => new GeminiFunctionParameterProperty
                            {
                                Type = kvp.Value.Type,
                                Description = kvp.Value.Description,
                                Enum = kvp.Value.Enum?.Select(e => e.ToString()!).ToList()
                            }
                        ),
                        Required = tool.Parameters.Required
                    }
                }).ToList()
            }
        };
    }

    public bool HasTool(string toolName)
    {
        return _tools.ContainsKey(toolName);
    }
}

