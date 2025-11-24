using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProductAssistant.Core.Services;

public class GeminiService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiService> _logger;
    private const string GeminiApiBaseUrl = "https://generativelanguage.googleapis.com/v1beta";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public GeminiService(HttpClient httpClient, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(GeminiApiBaseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(2);
    }

    public async Task<string> GenerateResponseAsync(
        string prompt,
        string apiKey,
        string? systemPrompt = null,
        List<ChatMessage>? conversationHistory = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Gemini API key is missing");
                return "Please provide a Gemini API key in settings to use the AI assistant.";
            }

            var contents = new List<GeminiContent>();

            // Add system instruction if provided
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                contents.Add(new GeminiContent
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new GeminiPart { Text = systemPrompt } }
                });
                contents.Add(new GeminiContent
                {
                    Role = "model",
                    Parts = new List<GeminiPart> { new GeminiPart { Text = "I understand. I'll help you with product shopping queries." } }
                });
            }

            // Add conversation history
            if (conversationHistory != null && conversationHistory.Any())
            {
                foreach (var msg in conversationHistory)
                {
                    contents.Add(new GeminiContent
                    {
                        Role = msg.Role == "user" ? "user" : "model",
                        Parts = new List<GeminiPart> { new GeminiPart { Text = msg.Content } }
                    });
                }
            }

            // Add current user message
            contents.Add(new GeminiContent
            {
                Role = "user",
                Parts = new List<GeminiPart> { new GeminiPart { Text = prompt } }
            });

            var request = new GeminiRequest
            {
                Contents = contents,
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = 0.9, // Increased for more creative, natural responses
                    MaxOutputTokens = 2048, // Increased for more detailed responses
                    TopP = 0.95 // Increased for more diverse vocabulary
                }
            };

            _logger.LogInformation("Sending request to Gemini API");

            var url = $"{GeminiApiBaseUrl}/models/gemini-2.0-flash:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: cancellationToken);

            if (result?.Candidates == null || !result.Candidates.Any())
            {
                _logger.LogWarning("Empty response from Gemini API");
                return "I apologize, but I couldn't generate a response. Please try again.";
            }

            var content = result.Candidates[0].Content?.Parts?[0]?.Text ?? string.Empty;
            _logger.LogInformation("Received response from Gemini API: {Length} characters", content.Length);

            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling Gemini API: {Message}", ex.Message);
            // Throw to let ConversationalAIService handle - no static messages
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling Gemini API");
            // Throw to let ConversationalAIService handle - no static messages
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            // Throw to let ConversationalAIService handle - no static messages
            throw;
        }
    }

    public async Task<LLMResponse> GenerateResponseWithToolsAsync(
        string prompt,
        string apiKey,
        List<GeminiTool>? tools = null,
        string? systemPrompt = null,
        List<ChatMessage>? conversationHistory = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Gemini API key is missing");
                return new LLMResponse
                {
                    Text = "Please provide a Gemini API key in settings to use the AI assistant."
                };
            }

            var contents = new List<GeminiContent>();

            // Add system instruction if provided
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                contents.Add(new GeminiContent
                {
                    Role = "user",
                    Parts = new List<GeminiPart> { new GeminiPart { Text = systemPrompt } }
                });
                contents.Add(new GeminiContent
                {
                    Role = "model",
                    Parts = new List<GeminiPart> { new GeminiPart { Text = "I understand. I'll help you with product shopping queries using the available tools." } }
                });
            }

            // Add conversation history with function calls/responses
            if (conversationHistory != null && conversationHistory.Any())
            {
                foreach (var msg in conversationHistory)
                {
                    var parts = new List<GeminiPart>();
                    
                    // Add text content if present
                    if (!string.IsNullOrEmpty(msg.Content))
                    {
                        parts.Add(new GeminiPart { Text = msg.Content });
                    }
                    
                    // Add function calls if present
                    if (msg.FunctionCalls != null && msg.FunctionCalls.Any())
                    {
                        foreach (var funcCall in msg.FunctionCalls)
                        {
                            parts.Add(new GeminiPart
                            {
                                FunctionCall = new GeminiFunctionCallPart
                                {
                                    Name = funcCall.Name,
                                    Args = funcCall.Args
                                }
                            });
                        }
                    }
                    
                    // Add function response if present
                    if (msg.FunctionResponse != null)
                    {
                        parts.Add(new GeminiPart
                        {
                            FunctionResponse = new GeminiFunctionResponsePart
                            {
                                Name = msg.FunctionResponse.Name,
                                Response = msg.FunctionResponse.Response
                            }
                        });
                    }

                    if (parts.Any())
                    {
                        contents.Add(new GeminiContent
                        {
                            Role = msg.Role == "user" ? "user" : "model",
                            Parts = parts
                        });
                    }
                }
            }

            // Add current user message
            contents.Add(new GeminiContent
            {
                Role = "user",
                Parts = new List<GeminiPart> { new GeminiPart { Text = prompt } }
            });

            var request = new GeminiRequest
            {
                Contents = contents,
                GenerationConfig = new GeminiGenerationConfig
                {
                    Temperature = 0.9, // Increased for more creative, natural responses
                    MaxOutputTokens = 2048,
                    TopP = 0.95 // Increased for more diverse vocabulary
                }
            };

            // Add tools if provided
            if (tools != null && tools.Any())
            {
                request.Tools = tools;
            }

            _logger.LogInformation("Sending request to Gemini API with {ToolCount} tools", tools?.Count ?? 0);

            var url = $"{GeminiApiBaseUrl}/models/gemini-2.0-flash:generateContent?key={apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, request, JsonOptions, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API returned {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
                response.EnsureSuccessStatusCode();
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOptions, cancellationToken: cancellationToken);

            if (result?.Candidates == null || !result.Candidates.Any())
            {
                _logger.LogWarning("Empty response from Gemini API");
                return new LLMResponse
                {
                    Text = "I apologize, but I couldn't generate a response. Please try again."
                };
            }

            var candidate = result.Candidates[0];
            var responseText = string.Empty;
            var functionCalls = new List<GeminiFunctionCall>();

            if (candidate.Content?.Parts != null)
            {
                foreach (var part in candidate.Content.Parts)
                {
                    if (!string.IsNullOrEmpty(part.Text))
                    {
                        responseText += part.Text;
                    }
                    
                    if (part.FunctionCall != null)
                    {
                        functionCalls.Add(new GeminiFunctionCall
                        {
                            Name = part.FunctionCall.Name,
                            Args = part.FunctionCall.Args ?? new Dictionary<string, object>()
                        });
                    }
                }
            }

            _logger.LogInformation("Received response from Gemini API: {Length} characters, {FunctionCallCount} function calls", 
                responseText.Length, functionCalls.Count);

            return new LLMResponse
            {
                Text = responseText,
                FunctionCalls = functionCalls
            };
        }
        catch (HttpRequestException ex)
        {
            // Log the actual error response from Gemini if available
            _logger.LogError(ex, "HTTP error calling Gemini API: {Message}", ex.Message);
            // Throw exception instead of returning static message - let ConversationalAIService handle it
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout calling Gemini API");
            // Throw exception instead of returning static message
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            // Throw to let ConversationalAIService handle - no static messages
            throw;
        }
    }

    public async Task<bool> IsAvailableAsync(string apiKey)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return false;
            }

            // Test with the same model endpoint used in GenerateResponseAsync
            // Use gemini-2.0-flash which is the model actually used in the API calls
            var url = $"{GeminiApiBaseUrl}/models/gemini-2.0-flash:generateContent?key={apiKey}";
            var testRequest = new GeminiRequest
            {
                Contents = new List<GeminiContent>
                {
                    new GeminiContent
                    {
                        Role = "user",
                        Parts = new List<GeminiPart> { new GeminiPart { Text = "test" } }
                    }
                }
            };
            
            var response = await _httpClient.PostAsJsonAsync(url, testRequest, CancellationToken.None);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking LLM service availability");
            return false;
        }
    }
}

// Gemini API Request/Response Models
internal class GeminiRequest
{
    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = new();

    [JsonPropertyName("generationConfig")]
    public GeminiGenerationConfig? GenerationConfig { get; set; }

    [JsonPropertyName("tools")]
    public List<GeminiTool>? Tools { get; set; }
}

internal class GeminiContent
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new();
}

internal class GeminiPart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("functionCall")]
    public GeminiFunctionCallPart? FunctionCall { get; set; }

    [JsonPropertyName("functionResponse")]
    public GeminiFunctionResponsePart? FunctionResponse { get; set; }
}

internal class GeminiFunctionCallPart
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public Dictionary<string, object>? Args { get; set; }
}

internal class GeminiFunctionResponsePart
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("response")]
    public object? Response { get; set; }
}

internal class GeminiGenerationConfig
{
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; }

    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; set; }

    [JsonPropertyName("topP")]
    public double TopP { get; set; }
}

internal class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }
}

internal class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
}

/// <summary>
/// Function response for conversation history
/// </summary>
public class GeminiFunctionResponse
{
    public string Name { get; set; } = string.Empty;
    public object? Response { get; set; }
}





