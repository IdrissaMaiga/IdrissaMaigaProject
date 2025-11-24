using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;
using System.Collections.Concurrent;

namespace ProductAssistant.Core.Services;

/// <summary>
/// Advanced tool executor with caching, parallel execution, retry logic, and analytics
/// </summary>
public class AdvancedToolExecutorService : IToolExecutorService
{
    private readonly Dictionary<string, IToolService> _tools;
    private readonly IMemoryCache? _cache;
    private readonly ILogger<AdvancedToolExecutorService> _logger;
    private readonly ConcurrentDictionary<string, ToolExecutionMetrics> _metrics;
    private readonly TimeSpan _defaultCacheExpiration = TimeSpan.FromMinutes(5);

    public AdvancedToolExecutorService(
        IEnumerable<IToolService> tools,
        ILogger<AdvancedToolExecutorService> logger,
        IMemoryCache? cache = null)
    {
        _logger = logger;
        _cache = cache;
        _tools = tools.ToDictionary(t => t.Name, t => t);
        _metrics = new ConcurrentDictionary<string, ToolExecutionMetrics>();
        
        _logger.LogInformation("Initialized AdvancedToolExecutorService with {Count} tools: {ToolNames}", 
            _tools.Count, string.Join(", ", _tools.Keys));
    }

    public async Task<ToolExecutionResult> ExecuteToolAsync(ToolCall toolCall, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var metrics = _metrics.GetOrAdd(toolCall.Name, _ => new ToolExecutionMetrics());

        try
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
                metrics.IncrementFailures();
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = $"Tool '{toolCall.Name}' not found",
                    ErrorCode = "TOOL_NOT_FOUND"
                };
            }

            // Check cache for cacheable tools
            var cacheKey = GetCacheKey(toolCall);
            if (_cache != null && cacheKey != null && IsCacheableTool(toolCall.Name))
            {
                if (_cache.TryGetValue(cacheKey, out ToolExecutionResult? cachedResult))
                {
                    _logger.LogInformation("Cache hit for tool: {ToolName}", toolCall.Name);
                    metrics.IncrementCacheHits();
                    return cachedResult!;
                }
            }

            // Execute tool with retry logic
            ToolExecutionResult? result = null;
            var maxRetries = 3;
            var retryDelay = TimeSpan.FromMilliseconds(500);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Executing tool: {ToolName} (attempt {Attempt}/{MaxRetries}) with arguments: {Arguments}", 
                        toolCall.Name, attempt, maxRetries, System.Text.Json.JsonSerializer.Serialize(toolCall.Arguments));
                    
                    result = await tool.ExecuteAsync(toolCall, cancellationToken);
                    
                    if (result.Success)
                    {
                        break;
                    }
                    
                    // If not successful and not last attempt, retry
                    if (attempt < maxRetries)
                    {
                        _logger.LogWarning("Tool execution failed, retrying in {Delay}ms: {Error}", 
                            retryDelay.TotalMilliseconds, result.ErrorMessage);
                        await Task.Delay(retryDelay, cancellationToken);
                        retryDelay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * 2); // Exponential backoff
                    }
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Error executing tool (attempt {Attempt}), retrying", attempt);
                    await Task.Delay(retryDelay, cancellationToken);
                    retryDelay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * 2);
                }
            }

            if (result == null)
            {
                metrics.IncrementFailures();
                return new ToolExecutionResult
                {
                    Success = false,
                    ErrorMessage = "Tool execution failed after retries",
                    ErrorCode = "EXECUTION_FAILED"
                };
            }

            // Cache successful results for cacheable tools
            if (result.Success && _cache != null && cacheKey != null && IsCacheableTool(toolCall.Name))
            {
                _cache.Set(cacheKey, result, _defaultCacheExpiration);
                _logger.LogDebug("Cached result for tool: {ToolName}", toolCall.Name);
            }

            var duration = DateTime.UtcNow - startTime;
            metrics.RecordExecution(duration, result.Success);

            _logger.LogInformation("Tool execution completed: {ToolName}, Success: {Success}, Duration: {Duration}ms", 
                toolCall.Name, result.Success, duration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing tool: {ToolName}", toolCall.Name);
            metrics.IncrementFailures();
            return new ToolExecutionResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "EXECUTION_ERROR"
            };
        }
    }

    public async Task<List<ToolExecutionResult>> ExecuteToolsParallelAsync(
        List<ToolCall> toolCalls, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing {Count} tools in parallel", toolCalls.Count);
        
        var tasks = toolCalls.Select(tc => ExecuteToolAsync(tc, cancellationToken));
        var results = await Task.WhenAll(tasks);
        
        return results.ToList();
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
                                Enum = kvp.Value.Enum?.Select(e => e.ToString()!).ToList(),
                                // For array types, set items based on the property name or default to integer
                                Items = kvp.Value.Type == "array" 
                                    ? new GeminiFunctionParameterItems 
                                    { 
                                        Type = kvp.Key.ToLower().Contains("id") || kvp.Key.ToLower().Contains("number") || kvp.Key.ToLower().Contains("count")
                                            ? "integer" 
                                            : "string" 
                                    }
                                    : null
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

    public ToolExecutionMetrics GetMetrics(string toolName)
    {
        return _metrics.GetOrAdd(toolName, _ => new ToolExecutionMetrics());
    }

    public Dictionary<string, ToolExecutionMetrics> GetAllMetrics()
    {
        return _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    private string? GetCacheKey(ToolCall toolCall)
    {
        try
        {
            var argsJson = System.Text.Json.JsonSerializer.Serialize(toolCall.Arguments);
            return $"tool:{toolCall.Name}:{argsJson.GetHashCode()}";
        }
        catch
        {
            return null;
        }
    }

    private bool IsCacheableTool(string toolName)
    {
        // Tools that can be safely cached (read-only operations)
        var cacheableTools = new[] { "get_product_details", "get_user_products", "get_price_analytics" };
        return cacheableTools.Contains(toolName);
    }
}

/// <summary>
/// Metrics for tool execution tracking
/// </summary>
public class ToolExecutionMetrics
{
    private long _executionCount;
    private long _successCount;
    private long _failureCount;
    private long _cacheHits;
    private readonly List<TimeSpan> _executionTimes = new();
    private readonly object _lock = new();

    public long ExecutionCount => _executionCount;
    public long SuccessCount => _successCount;
    public long FailureCount => _failureCount;
    public long CacheHits => _cacheHits;
    public double SuccessRate => _executionCount > 0 ? (double)_successCount / _executionCount * 100 : 0;
    public double AverageExecutionTime => _executionTimes.Any() 
        ? _executionTimes.Average(t => t.TotalMilliseconds) 
        : 0;

    public void RecordExecution(TimeSpan duration, bool success)
    {
        lock (_lock)
        {
            Interlocked.Increment(ref _executionCount);
            if (success)
            {
                Interlocked.Increment(ref _successCount);
            }
            else
            {
                Interlocked.Increment(ref _failureCount);
            }
            
            _executionTimes.Add(duration);
            if (_executionTimes.Count > 100) // Keep last 100 execution times
            {
                _executionTimes.RemoveAt(0);
            }
        }
    }

    public void IncrementFailures()
    {
        Interlocked.Increment(ref _executionCount);
        Interlocked.Increment(ref _failureCount);
    }

    public void IncrementCacheHits()
    {
        Interlocked.Increment(ref _cacheHits);
    }
}

