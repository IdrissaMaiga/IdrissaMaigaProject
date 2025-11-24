using Microsoft.EntityFrameworkCore;
using ProductAssistant.Api.Services;
using ProductAssistant.Core.Configuration;
using ProductAssistant.Core.Data;
using ProductAssistant.Core.Mappings;
using ProductAssistant.Core.Repositories;
using ProductAssistant.Core.Services;

namespace ProductAssistant.Api.Extensions;

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

    public static IServiceCollection AddProductAssistantRepositories(this IServiceCollection services)
    {
        // Repositories are already registered in Core.Extensions.ServiceCollectionExtensions
        // This method is kept for API-specific repository registrations if needed in the future
        return services;
    }

    public static IServiceCollection AddProductAssistantServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Repositories (must be registered before services that depend on them)
        services.AddProductAssistantRepositories();

        // Mapping service
        services.AddSingleton<IMappingService, MappingService>();

        // Core services
        services.AddScoped<IProductService, Services.ProductService>();
        services.AddScoped<IConversationMemoryService, ConversationMemoryService>();
        services.AddScoped<IProductComparisonService, ProductComparisonService>();

        // Token Service
        var authConfig = configuration.GetSection("Auth").Get<AuthConfiguration>()
            ?? new AuthConfiguration 
            { 
                SecretKey = configuration["Auth:SecretKey"] 
                    ?? "CHANGE-THIS-TO-A-SECURE-RANDOM-KEY-IN-PRODUCTION-MINIMUM-32-CHARACTERS-LONG" 
            };
        
        services.AddSingleton<ITokenService>(sp => new TokenService(authConfig.SecretKey));

        return services;
    }

    public static IServiceCollection AddProductAssistantApiServices(this IServiceCollection services, IConfiguration? configuration = null)
    {
        // Register repositories from Core
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductComparisonRepository, ProductComparisonRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationMessageRepository, ConversationMessageRepository>();

        // Register core services (but NOT ConversationalAIService - that's for AI Service only)
        services.AddScoped<IConversationMemoryService, ConversationMemoryService>();
        services.AddScoped<IProductComparisonService, ProductComparisonService>();
        // Note: IConversationalAIService is NOT registered here - API calls AI Service via HTTP

        // Mapping service
        services.AddSingleton<IMappingService, MappingService>();

        // Token Service (if configuration provided)
        if (configuration != null)
        {
            var authConfig = configuration.GetSection("Auth").Get<AuthConfiguration>()
                ?? new AuthConfiguration 
                { 
                    SecretKey = configuration["Auth:SecretKey"] 
                        ?? "CHANGE-THIS-TO-A-SECURE-RANDOM-KEY-IN-PRODUCTION-MINIMUM-32-CHARACTERS-LONG" 
                };
            
            services.AddSingleton<ITokenService>(sp => new TokenService(authConfig.SecretKey));
        }

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

        // AI Service - HTTP client for calling AI service
        // Base address is set to AI service URL, endpoints are /api/ai/chat and /api/ai/search
        // Increased timeout to allow for Gemini API calls and scraping operations
        var aiServiceUrl = configuration["AIService:Url"] ?? configuration["ServiceOptions:AIServiceUrl"] ?? "http://ai-service:8080";
        services.AddHttpClient<IConversationalAIService, Services.AIServiceClient>(client =>
        {
            client.BaseAddress = new Uri(aiServiceUrl);
            client.Timeout = TimeSpan.FromSeconds(90); // 90 seconds for AI processing (LLM + scraping)
        });

        // Gemini LLM Service (for direct Gemini API calls if needed)
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
    /// Adds Swagger/OpenAPI services for API project
    /// </summary>
    public static IServiceCollection AddProductAssistantSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        return services;
    }
}

