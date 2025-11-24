namespace ProductAssistant.Core.Models;

/// <summary>
/// Schema definition for tool parameters
/// </summary>
public class ToolParameterSchema
{
    public string Type { get; set; } = "object";
    public Dictionary<string, ToolParameterProperty> Properties { get; set; } = new();
    public List<string> Required { get; set; } = new();
}

/// <summary>
/// Property definition for a tool parameter
/// </summary>
public class ToolParameterProperty
{
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public object? Default { get; set; }
    public List<object>? Enum { get; set; }
}

/// <summary>
/// Represents a tool call from the AI
/// </summary>
public class ToolCall
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
    public string? CallId { get; set; }
}

/// <summary>
/// Result of executing a tool
/// </summary>
public class ToolExecutionResult
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Tool definition for Gemini API
/// </summary>
public class GeminiTool
{
    [System.Text.Json.Serialization.JsonPropertyName("functionDeclarations")]
    public List<GeminiFunctionDeclaration> FunctionDeclarations { get; set; } = new();
}

/// <summary>
/// Function declaration for Gemini API
/// </summary>
public class GeminiFunctionDeclaration
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("parameters")]
    public GeminiFunctionParameters Parameters { get; set; } = new();
}

/// <summary>
/// Function parameters schema for Gemini API
/// </summary>
public class GeminiFunctionParameters
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } = "object";
    
    [System.Text.Json.Serialization.JsonPropertyName("properties")]
    public Dictionary<string, GeminiFunctionParameterProperty> Properties { get; set; } = new();
    
    [System.Text.Json.Serialization.JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

/// <summary>
/// Property definition for Gemini function parameters
/// </summary>
public class GeminiFunctionParameterProperty
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("description")]
    public string? Description { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("enum")]
    public List<string>? Enum { get; set; }
    
    [System.Text.Json.Serialization.JsonPropertyName("items")]
    public GeminiFunctionParameterItems? Items { get; set; }
}

/// <summary>
/// Items definition for array types in Gemini function parameters
/// </summary>
public class GeminiFunctionParameterItems
{
    [System.Text.Json.Serialization.JsonPropertyName("type")]
    public string Type { get; set; } = "string";
}

/// <summary>
/// Tool call from Gemini API response
/// </summary>
public class GeminiFunctionCall
{
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [System.Text.Json.Serialization.JsonPropertyName("args")]
    public Dictionary<string, object> Args { get; set; } = new();
}

