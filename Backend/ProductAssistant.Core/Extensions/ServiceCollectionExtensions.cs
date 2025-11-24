using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Configuration;
using ProductAssistant.Core.Data;
using ProductAssistant.Core.HealthChecks;
using ProductAssistant.Core.Repositories;
using ProductAssistant.Core.Services;
using ProductAssistant.Core.Services.Tools;

namespace ProductAssistant.Core.Extensions;

/// <summary>
/// Extension methods for service registration
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds database services (DbContext and DbContextFactory) - Core implementation
    /// </summary>
    public static IServiceCollection AddProductAssistantCoreDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var dbPath = configuration["Database:Path"] ?? "productassistant.db";
        
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));
        
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        return services;
    }

    /// <summary>
    /// Adds CORS configuration
    /// </summary>
    public static IServiceCollection AddProductAssistantCors(
        this IServiceCollection services,
        string policyName = "AllowAll")
    {
        services.AddCors(options =>
        {
            options.AddPolicy(policyName, policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }

    /// <summary>
    /// Adds core business services
    /// </summary>
    public static IServiceCollection AddProductAssistantServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductComparisonRepository, ProductComparisonRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationMessageRepository, ConversationMessageRepository>();

        // Business Services (ProductService is in API project, not Core)
        services.AddScoped<IConversationMemoryService, ConversationMemoryService>();
        services.AddScoped<IProductComparisonService, ProductComparisonService>();
        services.AddScoped<IConversationalAIService, ConversationalAIService>();

        return services;
    }

    /// <summary>
    /// Adds HTTP clients for external services - Core implementation
    /// </summary>
    public static IServiceCollection AddProductAssistantCoreHttpClients(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // Scraping Service
        services.AddHttpClient<IArukeresoScrapingService, ArukeresoScrapingService>();

        // LLM Service (Gemini)
        services.AddHttpClient<ILLMService>(client =>
        {
            var timeoutMinutes = configuration?["AI:RequestTimeoutMinutes"] ?? "2";
            if (int.TryParse(timeoutMinutes, out var timeout))
            {
                client.Timeout = TimeSpan.FromMinutes(timeout);
            }
            else
            {
                client.Timeout = TimeSpan.FromMinutes(2);
            }
        }).AddTypedClient<ILLMService>((client, sp) =>
        {
            var logger = sp.GetRequiredService<ILogger<GeminiService>>();
            return new GeminiService(client, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds authentication services
    /// </summary>
    public static IServiceCollection AddProductAssistantAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var secretKey = configuration["Auth:SecretKey"] ?? 
            "CHANGE-THIS-TO-A-SECURE-RANDOM-KEY-IN-PRODUCTION-MINIMUM-32-CHARACTERS-LONG";
        
        services.AddSingleton<ITokenService>(sp => new TokenService(secretKey));

        return services;
    }

    /// <summary>
    /// Adds application configuration
    /// </summary>
    public static IServiceCollection AddProductAssistantConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.Configure<ServiceOptions>(configuration.GetSection(ServiceOptions.SectionName));

        return services;
    }

    /// <summary>
    /// Adds EndpointsApiExplorer for Swagger/OpenAPI
    /// Note: AddSwaggerGen should be called in the consuming project (API, AI Service, Scraping Service)
    /// after adding Swashbuckle.AspNetCore package
    /// </summary>
    public static IServiceCollection AddProductAssistantEndpointsApiExplorer(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        return services;
    }

    /// <summary>
    /// Adds authentication services (alias for AddProductAssistantAuth)
    /// </summary>
    public static IServiceCollection AddProductAssistantAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services.AddProductAssistantAuth(configuration);
    }

    /// <summary>
    /// Adds core services and repositories
    /// Note: This method registers repositories and core services, but IProductService should be registered separately in the API project
    /// </summary>
    public static IServiceCollection AddProductAssistantCoreServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductComparisonRepository, ProductComparisonRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationMessageRepository, ConversationMessageRepository>();

        // Business Services (excluding IProductService - should be registered in API project)
        services.AddScoped<IConversationMemoryService, ConversationMemoryService>();
        services.AddScoped<IProductComparisonService, ProductComparisonService>();
        services.AddScoped<IConversationalAIService, ConversationalAIService>();
        
        return services;
    }

    /// <summary>
    /// Adds AI service-specific services
    /// Note: IProductService must be registered separately (typically via HTTP client to API Service)
    /// </summary>
    public static IServiceCollection AddProductAssistantAIServices(this IServiceCollection services)
    {
        services.AddProductAssistantServices();
        
        // Register tool services (these may depend on IProductService which should be registered separately)
        services.AddScoped<IToolService, ProductAssistant.Core.Services.Tools.SearchProductsTool>();
        services.AddScoped<IToolService, ProductAssistant.Core.Services.Tools.GetProductDetailsTool>();
        services.AddScoped<IToolService, ProductAssistant.Core.Services.Tools.CompareProductsTool>();
        services.AddScoped<IToolService, ProductAssistant.Core.Services.Tools.GetUserProductsTool>();
        services.AddScoped<IToolService, ProductAssistant.Core.Services.Tools.FilterProductsTool>();
        services.AddScoped<IToolService, ProductAssistant.Core.Services.Tools.GetProductRecommendationsTool>();
        services.AddScoped<IToolService, ProductAssistant.Core.Services.Tools.GetPriceAnalyticsTool>();
        
        // Register tool executor (use AdvancedToolExecutorService with memory cache)
        services.AddMemoryCache();
        services.AddScoped<IToolExecutorService>(sp =>
        {
            var tools = sp.GetServices<IToolService>();
            var logger = sp.GetRequiredService<ILogger<AdvancedToolExecutorService>>();
            var cache = sp.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            return new AdvancedToolExecutorService(tools, logger, cache);
        });
        
        return services;
    }

    /// <summary>
    /// Adds health check services
    /// </summary>
    public static IServiceCollection AddProductAssistantHealthChecks(
        this IServiceCollection services,
        bool includeDatabase = true)
    {
        var healthChecks = services.AddHealthChecks();
        
        if (includeDatabase)
        {
            healthChecks.AddCheck<DatabaseHealthCheck>("database");
        }
        
        return services;
    }

    /// <summary>
    /// Adds scraping service with a specific implementation
    /// </summary>
    public static IServiceCollection AddProductAssistantScrapingService<TImplementation>(
        this IServiceCollection services)
        where TImplementation : class, IArukeresoScrapingService
    {
        services.AddHttpClient<IArukeresoScrapingService, TImplementation>();
        return services;
    }
}
