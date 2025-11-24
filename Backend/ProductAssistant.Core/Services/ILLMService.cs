using ProductAssistant.Core.Models;

namespace ProductAssistant.Core.Services;

public interface ILLMService
{
    Task<string> GenerateResponseAsync(string prompt, string apiKey, string? systemPrompt = null, List<ChatMessage>? conversationHistory = null, CancellationToken cancellationToken = default);
    Task<LLMResponse> GenerateResponseWithToolsAsync(
        string prompt, 
        string apiKey, 
        List<GeminiTool>? tools = null,
        string? systemPrompt = null, 
        List<ChatMessage>? conversationHistory = null, 
        CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(string apiKey);
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public List<GeminiFunctionCall>? FunctionCalls { get; set; }
    public GeminiFunctionResponse? FunctionResponse { get; set; }
}

public class LLMResponse
{
    public string Text { get; set; } = string.Empty;
    public List<GeminiFunctionCall> FunctionCalls { get; set; } = new();
    public bool HasFunctionCalls => FunctionCalls.Any();
}

