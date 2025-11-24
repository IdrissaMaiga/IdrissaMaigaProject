using Microsoft.EntityFrameworkCore;
using ProductAssistant.Core.Configuration;
using ProductAssistant.Core.Data;
using ProductAssistant.Core.Services;
using ProductAssistant.Core.Services.Tools;

namespace ProductAssistant.AIService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProductAssistantDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var dbConfig = configuration.GetSection("Database").Get<DatabaseConfiguration>() 
            ?? new DatabaseConfiguration { Path = configuration["Database:Path"] ?? "productassistant.db" };
        
        var connectionString = $"Data Source={dbConfig.Path}";
        
        // Register DbContext for direct injection (scoped)
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }

    public static IServiceCollection AddProductAssistantServices(this IServiceCollection services)
    {
        services.AddScoped<IConversationMemoryService, ConversationMemoryService>();
        services.AddScoped<IConversationalAIService, ConversationalAIService>();

        return services;
    }

    public static IServiceCollection AddProductAssistantAIServices(this IServiceCollection services)
    {
        // Core services
        services.AddScoped<IConversationMemoryService, ConversationMemoryService>();
        
        // Product service - needed by tools
        services.AddHttpClient<IProductService, ProductService>();
        
        // Tool services - register all tools
        services.AddScoped<IToolService, SearchProductsTool>();
        services.AddScoped<IToolService, GetProductDetailsTool>();
        services.AddScoped<IToolService, CompareProductsTool>();
        services.AddScoped<IToolService, GetUserProductsTool>();
        
        // Tool executor - collects all IToolService implementations
        services.AddScoped<IToolExecutorService, ToolExecutorService>();
        
        // AI service - depends on tool executor
        services.AddScoped<IConversationalAIService, ConversationalAIService>();

        return services;
    }

    public static IServiceCollection AddProductAssistantHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        // Scraping Service
        var scrapingServiceUrl = configuration["ScrapingService:Url"] ?? "http://scraping-service:8080";
        services.AddHttpClient<IArukeresoScrapingService, ArukeresoScrapingService>(client =>
        {
            client.BaseAddress = new Uri(scrapingServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Gemini LLM Service
        services.AddHttpClient<ILLMService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
        }).AddTypedClient<ILLMService>((client, sp) =>
        {
            var logger = sp.GetRequiredService<ILogger<GeminiService>>();
            return new GeminiService(client, logger);
        });

        return services;
    }

    public static IServiceCollection AddProductAssistantCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }

    /// <summary>
    /// Adds Swagger/OpenAPI services for AI Service
    /// </summary>
    public static IServiceCollection AddProductAssistantSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }
}

