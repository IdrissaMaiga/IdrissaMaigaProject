namespace ProductAssistant.Core.Configuration;

/// <summary>
/// Application configuration settings
/// </summary>
public class AppSettings
{
    public DatabaseSettings Database { get; set; } = new();
    public AuthSettings Auth { get; set; } = new();
    public AISettings AI { get; set; } = new();
    public ServiceUrls ServiceUrls { get; set; } = new();
}

public class DatabaseSettings
{
    public string Path { get; set; } = "productassistant.db";
}

public class AuthSettings
{
    public string SecretKey { get; set; } = string.Empty;
}

public class AISettings
{
    public string SystemPrompt { get; set; } = @"You are a helpful AI assistant for a product shopping application. 
Your role is to help users find and compare products from e-commerce sites.

Key capabilities:
- Search for products based on user queries
- Compare prices and features
- Provide product recommendations
- Answer questions about products

Guidelines:
- Be concise and helpful
- If asked about products, search for them first
- Provide price comparisons when relevant
- Use clear, simple English
- If you don't know something, admit it and suggest searching for products";

    public string ComparisonPrompt { get; set; } = @"You are an expert product comparison assistant. 
Your task is to analyze and compare products objectively, highlighting:
- Price differences and value propositions
- Key features and specifications
- Store reliability and availability
- Best overall recommendation

Be concise, clear, and helpful in your comparisons.";

    public int RequestTimeoutMinutes { get; set; } = 2;
}

public class ServiceUrls
{
    public string ApiService { get; set; } = "http://localhost:5000";
    public string AIService { get; set; } = "http://localhost:5003";
    public string ScrapingService { get; set; } = "http://localhost:5002";
}

