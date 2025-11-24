using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Models;
using ChatMessage = ProductAssistant.Core.Services.ChatMessage;

namespace ProductAssistant.Core.Services;

public class ConversationalAIService : IConversationalAIService
{
    private readonly IArukeresoScrapingService _scrapingService;
    private readonly ILLMService _llmService;
    private readonly IConversationMemoryService _memoryService;
    private readonly IToolExecutorService _toolExecutor;
    private readonly ILogger<ConversationalAIService> _logger;

    private const string SystemPrompt = @"You are a helpful AI shopping assistant. Be friendly, conversational, and PROACTIVE.

KEY BEHAVIORS:
- Understand context and remember previous messages in the conversation
- DO NOT ask for confirmations - just search immediately when user mentions products
- When user mentions ANY product, brand, color, model, or specification, SEARCH IMMEDIATELY
- Provide insights and recommendations, not just lists
- Highlight best value, cheapest options, and key differences
- Be enthusiastic about helping users find great deals

CRITICAL TOOL USAGE:
- ALWAYS use search_products when users mention ANY product, brand, or item
- DO NOT ask ""what color?"", ""what model?"", ""what storage?"" - just search with what they gave you
- If user says ""iPhone"", search for ""iPhone"" immediately
- If user says ""blue iPhone 14 128GB"", search for ""iPhone 14 128GB blue"" immediately
- Use compare_products to compare multiple products
- Use filter_products to narrow results by price or category
- Use get_product_recommendations for AI-powered personalized suggestions based on conversation context
- NEVER make up product information - always use tools first

PROACTIVE SEARCHING:
- User says ""iPhone"" → SEARCH ""iPhone"" immediately (don't ask which model)
- User says ""laptop"" → SEARCH ""laptop"" immediately (don't ask for budget first)
- User says ""14 plus blue 128gb"" → SEARCH ""iPhone 14 plus 128gb blue"" immediately
- User says ""yes"" after you asked about specs → SEARCH with those specs immediately
- NEVER say ""I'll search for..."" and then NOT search - ALWAYS actually call search_products tool

PRODUCT RECOMMENDATIONS:
- When user asks for recommendations/suggestions ('what should I buy', 'recommend me', 'suggest'), use get_product_recommendations tool
- Provide conversationContext parameter summarizing what they're looking for (e.g., 'budget gaming laptop', 'iPhone for photography')
- Include price range if mentioned in conversation (maxPrice, minPrice)
- Recommend MULTIPLE products (3-5 typically) with detailed explanations for EACH
- Explain WHY each product is recommended based on their needs
- Compare the recommended products and help user decide
- Be specific about features, value, and trade-offs

RESPONSE STYLE:
- Start by acknowledging the request and IMMEDIATELY search
- Provide conversational responses with context
- When recommending products, explain each recommendation in detail
- End with helpful follow-up questions AFTER showing results
- Use markdown for formatting
- Be DECISIVE and PROACTIVE - don't ask for permission to search

MANDATORY: When user mentions products (laptop, iPhone, Mac, etc) or says find/look/need/want/show, IMMEDIATELY use search_products tool WITHOUT asking for confirmation.";

    public ConversationalAIService(
        IArukeresoScrapingService scrapingService,
        ILLMService llmService,
        IConversationMemoryService memoryService,
        IToolExecutorService toolExecutor,
        ILogger<ConversationalAIService> logger)
    {
        _scrapingService = scrapingService;
        _llmService = llmService;
        _memoryService = memoryService;
        _toolExecutor = toolExecutor;
        _logger = logger;
    }

    public async Task<ConversationalResponse> GetResponseAsync(
        string userMessage, 
        string userId, 
        string? apiKey = null,
        List<Product>? contextProducts = null, 
        int? conversationId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var hasApiKey = !string.IsNullOrWhiteSpace(apiKey);
            _logger.LogInformation("Processing user message: {Message} for user: {UserId}, Conversation: {ConversationId}, HasApiKey: {HasApiKey}, ApiKeyLength: {ApiKeyLength}", 
                userMessage, userId, conversationId, hasApiKey, apiKey?.Length ?? 0);

            // Require API key - AI only, no static fallbacks
            if (!hasApiKey)
            {
                _logger.LogWarning("API key is null or empty - cannot use LLM service");
                return new ConversationalResponse
                {
                    Response = "Please configure your Gemini API key in Settings to use the AI assistant.",
                    Products = new List<Product>()
                };
            }
            
            // Use AI with tools - no static responses
            // Get conversation history using conversationId - load more messages for better context
            List<ConversationMessage> history = new();
            if (conversationId.HasValue)
            {
                try
                {
                    // Load more messages (50) to provide better context to the AI
                    history = await _memoryService.GetConversationHistoryAsync(conversationId.Value, 50);
                    _logger.LogInformation("Loaded {Count} messages from conversation history", history.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load conversation history for conversation {ConversationId}", conversationId);
                }
            }
            
            // Convert to ChatMessage format with function call support
            // Ensure we include both user messages and assistant responses in chronological order
            var conversationHistory = new List<ChatMessage>();
            foreach (var m in history.OrderBy(h => h.CreatedAt).ThenBy(h => h.Id))
            {
                // Add user message if it exists
                if (m.IsUserMessage && !string.IsNullOrWhiteSpace(m.Message))
                {
                    var userChatMsg = new ChatMessage
                    {
                        Role = "user",
                        Content = m.Message
                    };
                    conversationHistory.Add(userChatMsg);
                }
                
                // Add assistant response if it exists
                if (!m.IsUserMessage && !string.IsNullOrWhiteSpace(m.Response))
                {
                    var assistantChatMsg = new ChatMessage
                    {
                        Role = "assistant",
                        Content = m.Response
                    };
                    conversationHistory.Add(assistantChatMsg);
                }
            }

            // Build enhanced system prompt with product context
            var enhancedSystemPrompt = SystemPrompt;
            if (contextProducts != null && contextProducts.Any())
            {
                enhancedSystemPrompt += $"\n\nCurrent product context (user's saved products):\n";
                foreach (var product in contextProducts.Take(5))
                {
                    enhancedSystemPrompt += $"- {product.Name}: {product.Price:N0} {product.Currency}\n";
                }
            }

            // Get available tools
            var availableTools = _toolExecutor.GetAvailableTools();
            _logger.LogInformation("Using {ToolCount} tools for AI response", availableTools.Count);

            // Execute tool calling loop (max 5 iterations to prevent infinite loops)
            var maxIterations = 5;
            var iteration = 0;
            var finalResponse = string.Empty;
            var allProducts = new List<Product>();
            var currentHistory = new List<ChatMessage>(conversationHistory);

            while (iteration < maxIterations)
            {
                iteration++;
                _logger.LogInformation("Tool calling iteration {Iteration}/{MaxIterations}", iteration, maxIterations);

                // Add user message for this iteration
                if (iteration == 1)
                {
                    currentHistory.Add(new ChatMessage
                    {
                        Role = "user",
                        Content = userMessage
                    });
                }

                // Call LLM with tools
                // After tool execution, prompt the AI to provide a conversational response
                var prompt = iteration == 1 
                    ? userMessage 
                    : $"Based on the tool results above, provide a helpful conversational response to the user's original question: '{userMessage}'. Explain what you found in a friendly, natural way, as if you're a shop assistant talking to a customer. Be specific about the products or information you discovered.";
                
                var llmResponse = await _llmService.GenerateResponseWithToolsAsync(
                    prompt,
                    apiKey!,
                    availableTools,
                    enhancedSystemPrompt,
                    currentHistory,
                    cancellationToken);

                // Add LLM response to history
                var assistantMessage = new ChatMessage
                {
                    Role = "assistant",
                    Content = llmResponse.Text,
                    FunctionCalls = llmResponse.FunctionCalls
                };
                currentHistory.Add(assistantMessage);

                // If no function calls, we're done
                if (!llmResponse.HasFunctionCalls)
                {
                    finalResponse = llmResponse.Text;
                    _logger.LogInformation("AI provided final response without function calls");
                    break;
                }

                // If we have text response along with function calls, use it as base
                if (!string.IsNullOrWhiteSpace(llmResponse.Text))
                {
                    finalResponse = llmResponse.Text;
                }

                // Execute function calls
                _logger.LogInformation("Executing {Count} function calls", llmResponse.FunctionCalls.Count);
                foreach (var functionCall in llmResponse.FunctionCalls)
                {
                    var toolCall = new ToolCall
                    {
                        Name = functionCall.Name,
                        Arguments = functionCall.Args,
                        CallId = Guid.NewGuid().ToString()
                    };

                    var toolResult = await _toolExecutor.ExecuteToolAsync(toolCall, cancellationToken);

                    // Extract products from tool results if available
                    if (toolResult.Success && toolResult.Result != null)
                    {
                        var resultJson = System.Text.Json.JsonSerializer.Serialize(toolResult.Result);
                        var resultDict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(resultJson);
                        
                        if (resultDict != null && resultDict.TryGetValue("products", out var productsObj))
                        {
                            var productsJson = System.Text.Json.JsonSerializer.Serialize(productsObj);
                            var options = new System.Text.Json.JsonSerializerOptions 
                            { 
                                PropertyNameCaseInsensitive = true 
                            };
                            var products = System.Text.Json.JsonSerializer.Deserialize<List<Product>>(productsJson, options);
                            if (products != null)
                            {
                                _logger.LogInformation("Deserialized {Count} products from tool result. First product: Name={Name}, Price={Price}", 
                                    products.Count, products.FirstOrDefault()?.Name, products.FirstOrDefault()?.Price);
                                allProducts.AddRange(products);
                            }
                        }
                    }

                    // Add function response to history
                    var functionResponse = new GeminiFunctionResponse
                    {
                        Name = functionCall.Name,
                        Response = toolResult.Success ? toolResult.Result : new { error = toolResult.ErrorMessage, code = toolResult.ErrorCode }
                    };

                    currentHistory.Add(new ChatMessage
                    {
                        Role = "user",
                        FunctionResponse = functionResponse
                    });
                }

                // After executing tools, we need to get a conversational response from the AI
                // Continue the loop to get the final response (unless we've hit max iterations)
                if (iteration < maxIterations)
                {
                    // The loop will continue and call the LLM again with the tool results
                    // This allows the AI to generate a conversational response about the results
                    continue;
                }

                // If this was the last iteration, use the text response
                if (iteration >= maxIterations)
                {
                    finalResponse = llmResponse.Text;
                    if (string.IsNullOrWhiteSpace(finalResponse))
                    {
                        finalResponse = "I've processed your request using the available tools. Please see the results above.";
                    }
                }
            }

            // Ensure we have a conversational response, especially when products were found
            if (string.IsNullOrWhiteSpace(finalResponse))
            {
                if (allProducts.Any())
                {
                    finalResponse = $"I found {allProducts.Count} product(s) for you! Here are the results:";
                }
                else
                {
                    finalResponse = "I've processed your request. How can I help you further?";
                }
            }
            else if (allProducts.Any() && !finalResponse.Contains("found", StringComparison.OrdinalIgnoreCase) && 
                     !finalResponse.Contains("product", StringComparison.OrdinalIgnoreCase))
            {
                // If we have products but the response doesn't mention them, enhance it
                finalResponse = $"{finalResponse}\n\nI found {allProducts.Count} product(s) that match your request:";
            }

            // Save conversation to memory only if conversationId is provided
            if (conversationId.HasValue)
            {
                try
                {
                    // Check for duplicate user message within last 5 seconds to prevent duplicates
                    var recentHistory = await _memoryService.GetConversationHistoryAsync(conversationId.Value, 5);
                    var recentUserMessage = recentHistory
                        .Where(m => m.IsUserMessage && 
                                    m.Message == userMessage && 
                                    (DateTime.UtcNow - m.CreatedAt).TotalSeconds < 5)
                        .FirstOrDefault();
                    
                    if (recentUserMessage == null)
                    {
                        // Save user message separately
                        var userMessageRecord = new ConversationMessage
                        {
                            ConversationId = conversationId.Value,
                            UserId = userId,
                            Message = userMessage,
                            Response = string.Empty,
                            IsUserMessage = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _memoryService.SaveMessageAsync(userMessageRecord);
                    }
                    else
                    {
                        _logger.LogInformation("Skipping duplicate user message within 5 seconds");
                    }
                    
                    // Save assistant response separately with products
                    var distinctProducts = allProducts.DistinctBy(p => p.Id).ToList();
                    var productsJson = distinctProducts.Any() 
                        ? System.Text.Json.JsonSerializer.Serialize(distinctProducts)
                        : null;
                    var productIdsJson = distinctProducts.Any() 
                        ? System.Text.Json.JsonSerializer.Serialize(distinctProducts.Select(p => p.Id).ToList())
                        : null;
                    
                    _logger.LogInformation("Saving assistant response with {ProductCount} products. ProductsJson length: {Length}", 
                        distinctProducts.Count, productsJson?.Length ?? 0);
                    
                    var assistantMessageRecord = new ConversationMessage
                    {
                        ConversationId = conversationId.Value,
                        UserId = userId,
                        Message = string.Empty,
                        Response = finalResponse,
                        IsUserMessage = false,
                        CreatedAt = DateTime.UtcNow,
                        ProductsJson = productsJson,
                        ProductIdsJson = productIdsJson
                    };
                    await _memoryService.SaveMessageAsync(assistantMessageRecord);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving conversation message");
                }
            }

            return new ConversationalResponse
            {
                Response = finalResponse,
                Products = allProducts.DistinctBy(p => p.Id).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing user message with LLM: {Error}", ex.Message);
            
            // Try AI extraction for search terms even on error
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                try
                {
                    var searchTerm = await ExtractSearchTermAIAsync(userMessage, apiKey, cancellationToken);
                    if (!string.IsNullOrEmpty(searchTerm))
                    {
                        var products = await _scrapingService.SearchProductsAsync(searchTerm, cancellationToken);
                        if (products.Any())
                        {
                            return new ConversationalResponse
                            {
                                Response = $"I found {products.Count} product(s) for '{searchTerm}'. Here are all the results:",
                                Products = products // Return ALL products, no limit
                            };
                        }
                    }
                }
                catch (Exception extractEx)
                {
                    _logger.LogError(extractEx, "Error in AI extraction fallback");
                }
            }
            
            // Last resort: return error message asking user to retry
            return new ConversationalResponse
            {
                Response = "I'm having trouble processing your request right now. Please try rephrasing your question or check your API key in settings.",
                Products = new List<Product>()
            };
        }
    }

    // Removed GetFallbackResponseAsync - all responses now use AI only

    public async Task<List<Product>> SearchProductsFromQueryAsync(string query, string? apiKey = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var searchTerm = await ExtractSearchTermAIAsync(query, apiKey, cancellationToken);
            if (string.IsNullOrEmpty(searchTerm))
            {
                _logger.LogWarning("Could not extract search term from query: {Query} - UI should prompt user to retry", query);
                // Return empty list - UI will handle prompting user to retry/clarify
                return new List<Product>();
            }

            _logger.LogInformation("Searching products with AI-extracted term: {SearchTerm} (from query: {Query})", searchTerm, query);
            var products = await _scrapingService.SearchProductsAsync(searchTerm, cancellationToken);
            _logger.LogInformation("Found {Count} products for search term: {SearchTerm}", products.Count, searchTerm);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products from query: {Query}", query);
            // Return empty list - UI will handle error display and retry
            return new List<Product>();
        }
    }

    private async Task<string?> ExtractSearchTermAIAsync(string message, string? apiKey = null, CancellationToken cancellationToken = default)
    {
        try
    {
            // Only use AI extraction - no static fallback
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("API key not provided for search term extraction");
                return null;
            }

            var isLLMAvailable = await _llmService.IsAvailableAsync(apiKey);
            if (!isLLMAvailable)
            {
                _logger.LogWarning("LLM service not available for search term extraction");
                return null;
            }

            var extractionPrompt = $@"Extract the product search term from this user query. 
Return ONLY the essential product keywords that should be used for searching, without any extra words, verbs, or filler words.

Examples:
- ""Find me a laptop under 50000 HUF"" → ""laptop""
- ""I need smartphones with good camera"" → ""smartphone camera""
- ""Show me wireless headphones"" → ""wireless headphones""
- ""Looking for tablets"" → ""tablet""
- ""I want to buy an iPhone"" → ""iPhone""

User query: ""{message}""

Extract the search term (only the product keywords, 1-3 words max):";

            var systemPrompt = "You are a search term extraction assistant. Extract only the essential product keywords from user queries. Return ONLY the search term, nothing else.";

            var extractedTerm = await _llmService.GenerateResponseAsync(
                extractionPrompt,
                apiKey,
                systemPrompt,
                null,
                cancellationToken);

            // Clean up the AI response - remove quotes, extra whitespace, and ensure it's just the term
            var cleanedTerm = extractedTerm
                .Trim()
                .Trim('"', '\'', '`', '.', '!', '?')
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault()?
                .Trim()
                .Trim('"', '\'', '`', '.', '!', '?') ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(cleanedTerm) && cleanedTerm.Length >= 2)
            {
                _logger.LogInformation("AI extracted search term: '{ExtractedTerm}' from query: '{Query}'", cleanedTerm, message);
                return cleanedTerm;
            }

            _logger.LogWarning("AI extraction returned empty or invalid term for query: {Query}", message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI-based search term extraction for query: {Query}", message);
            return null;
        }
    }

    public async Task<string> CompareProductsAsync(
        List<Product> products, 
        string? apiKey = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (products == null || products.Count < 2)
            {
                return "Please provide at least 2 products to compare.";
            }

            _logger.LogInformation("Comparing {Count} products using AI", products.Count);

            // Use AI only - no static fallbacks
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("API key is required for product comparison");
                return "Please configure your Gemini API key in Settings to compare products.";
            }

            // Build detailed product information for comparison
            var productDetails = new System.Text.StringBuilder();
            productDetails.AppendLine("Please compare the following products in detail:\n");
            
            for (int i = 0; i < products.Count; i++)
            {
                var product = products[i];
                productDetails.AppendLine($"Product {i + 1}:");
                productDetails.AppendLine($"  Name: {product.Name}");
                productDetails.AppendLine($"  Price: {product.Price:N0} {product.Currency}");
                if (!string.IsNullOrEmpty(product.StoreName))
                {
                    productDetails.AppendLine($"  Store: {product.StoreName}");
                }
                productDetails.AppendLine();
            }

            productDetails.AppendLine("Please provide a comprehensive comparison including:");
            productDetails.AppendLine("- Price comparison and value analysis");
            productDetails.AppendLine("- Feature differences (if descriptions are available)");
            productDetails.AppendLine("- Store/availability considerations");
            productDetails.AppendLine("- Overall recommendation based on the information provided");

            var comparisonPrompt = productDetails.ToString();

            var systemPrompt = @"You are an expert product comparison assistant. 
Your task is to analyze and compare products objectively, highlighting:
- Price differences and value propositions
- Key features and specifications
- Store reliability and availability
- Best overall recommendation

Be concise, clear, and helpful in your comparisons.";

            // Generate AI comparison
            var comparison = await _llmService.GenerateResponseAsync(
                comparisonPrompt,
                apiKey!,
                systemPrompt,
                null,
                cancellationToken);

            _logger.LogInformation("Generated AI comparison with {Length} characters", comparison.Length);
            return comparison;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI comparison: {Error}", ex.Message);
            return $"I encountered an error while comparing products: {ex.Message}. Please try again or check your API key.";
        }
    }

    private string FormatProductResponse(List<Product> products)
    {
        if (!products.Any())
        {
            return "I couldn't find any products.";
        }

        var response = $"I found {products.Count} product(s):\n\n";
        foreach (var product in products)
        {
            response += $"• {product.Name}\n";
            response += $"  Price: {product.Price:N0} {product.Currency}\n";
            if (!string.IsNullOrEmpty(product.StoreName))
            {
                response += $"  Store: {product.StoreName}\n";
            }
            response += "\n";
        }

        return response;
    }
}

