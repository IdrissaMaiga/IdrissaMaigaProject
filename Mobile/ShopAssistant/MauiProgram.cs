using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductAssistant.Core.Data;
using ProductAssistant.Core.Services;
using ShopAssistant.Services;
using ShopAssistant.ViewModels;
using ShopAssistant.Views;

namespace ShopAssistant;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Load configuration from appsettings.json
		try
		{
			var assembly = typeof(MauiProgram).Assembly;
			using var stream = assembly.GetManifestResourceStream("ShopAssistant.appsettings.json");
			if (stream != null)
			{
				var configBuilder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
				configBuilder.AddJsonStream(stream);
				var config = configBuilder.Build();
				ServiceUrlHelper.Initialize(config);
			}
		}
		catch
		{
			// If config loading fails, ServiceUrlHelper will use defaults
		}

		// SQLite Database for MAUI app - Use Factory pattern for Singleton services
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "shopassistant.db");
		builder.Services.AddDbContextFactory<AppDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"));
		
		// Also add regular DbContext for views that need it directly
		builder.Services.AddDbContext<AppDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"), ServiceLifetime.Scoped);

		// Configure API URLs from appsettings.json
		// URLs are configured in appsettings.json and can be updated via update-app-config.ps1
		var apiBaseUrl = ServiceUrlHelper.GetApiBaseUrl();
		var aiServiceUrl = ServiceUrlHelper.GetAIServiceUrl();
		var scrapingServiceUrl = ServiceUrlHelper.GetScrapingServiceUrl();
		
		// Extract base URLs (without /api paths)
		var apiServiceBase = new Uri(apiBaseUrl).GetLeftPart(UriPartial.Authority);
		var aiServiceBase = aiServiceUrl.Contains("/api/ai") 
			? aiServiceUrl.Replace("/api/ai", "") 
			: new Uri(aiServiceUrl).GetLeftPart(UriPartial.Authority);
		var scrapingServiceBase = scrapingServiceUrl.Contains("/api/scraping")
			? scrapingServiceUrl.Replace("/api/scraping", "")
			: new Uri(scrapingServiceUrl).GetLeftPart(UriPartial.Authority);

		// Register HTTP clients with base addresses
		// Increased timeout for ApiService to allow for AI processing time
		// Chain: Mobile -> API Service -> AI Service -> Gemini/Scraping (can take 60-90s total)
		// Base URL includes port, endpoints include /api prefix
		builder.Services.AddHttpClient("ApiService", client =>
		{
			client.BaseAddress = new Uri(apiBaseUrl); // http://localhost:8080
			client.Timeout = TimeSpan.FromSeconds(120); // 2 minutes to allow full AI processing chain
		})
		.AddHttpMessageHandler(serviceProvider =>
		{
			var authService = serviceProvider.GetRequiredService<ProductAssistant.Core.Services.IAuthService>();
			return new Services.AuthenticatedHttpMessageHandler(authService);
		});

		builder.Services.AddHttpClient("AIService", client =>
		{
			client.BaseAddress = new Uri(aiServiceBase);
			client.Timeout = TimeSpan.FromSeconds(60);
		});

		builder.Services.AddHttpClient("ScrapingService", client =>
		{
			client.BaseAddress = new Uri(scrapingServiceBase);
			client.Timeout = TimeSpan.FromSeconds(30);
		});

		builder.Services.AddHttpClient<IArukeresoScrapingService, ArukeresoScrapingService>(client =>
		{
			client.BaseAddress = new Uri(scrapingServiceBase);
			client.Timeout = TimeSpan.FromSeconds(30);
		});
		builder.Services.AddHttpClient<IProductService, ProductService>(client =>
		{
			// Base URL is host:8080, endpoints will use /api/products
			client.BaseAddress = new Uri(apiBaseUrl);
			client.Timeout = TimeSpan.FromSeconds(30);
		});
		builder.Services.AddHttpClient<ILLMService, GeminiService>(client =>
		{
			client.Timeout = TimeSpan.FromMinutes(2);
		});
		builder.Services.AddHttpClient<IAuthService, Auth0Service>(client =>
		{
			// Base URL is host:8080, endpoints will use /api/auth
			client.BaseAddress = new Uri(apiBaseUrl);
			client.Timeout = TimeSpan.FromSeconds(30);
		});

		// Register services as Singleton
		var settingsService = new SettingsService();
		builder.Services.AddSingleton<SettingsService>(settingsService);
		// Use client service that calls API endpoints - uses "ApiService" HttpClient
		builder.Services.AddSingleton<IConversationMemoryService, ConversationMemoryClientService>();
		builder.Services.AddSingleton<IConversationalAIService, ConversationalAIClientService>();
		builder.Services.AddSingleton<INetworkService, NetworkService>();
		builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
		
		// Register DebugLogService and initialize DebugHelper
		var debugLogService = new Services.DebugLogService();
		builder.Services.AddSingleton<Services.DebugLogService>(debugLogService);
		Services.DebugHelper.Initialize(debugLogService);

		// Register ViewModels as Transient (new instance per navigation - MAUI best practice)
		builder.Services.AddTransient<ChatViewModel>();
		builder.Services.AddTransient<CollectionViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<DebugLogViewModel>();

		// Register Views as Transient (new instance per navigation)
		builder.Services.AddTransient<ChatPage>();
		builder.Services.AddTransient<CollectionPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<SettingsPage>();
		builder.Services.AddTransient<Views.DebugLogPage>();
		
		// Register AppShell as Singleton
		builder.Services.AddSingleton<AppShell>();

#if DEBUG
		// Configure logging to show only important messages for UI debugging
		builder.Logging.AddDebug();
		
		// Set default to Warning - only show warnings and errors
		builder.Logging.SetMinimumLevel(LogLevel.Warning);
		
		// Allow Information level only for our application code
		builder.Logging.AddFilter("ShopAssistant", LogLevel.Information);
		builder.Logging.AddFilter("ProductAssistant", LogLevel.Information);
		
		// Suppress verbose framework logs - only show errors
		builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Error);
		builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None); // Suppress SQL commands
		builder.Logging.AddFilter("System.Net.Http", LogLevel.Warning); // Only HTTP warnings/errors
		builder.Logging.AddFilter("Microsoft.Maui", LogLevel.Warning);
		builder.Logging.AddFilter("Microsoft.Extensions", LogLevel.Warning);
		builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
		
		// Suppress verbose XAML binding diagnostics (warnings about x:DataType mismatches are common and not critical)
		builder.Logging.AddFilter("Microsoft.Maui.Controls.Xaml.Diagnostics.BindingDiagnostics", LogLevel.Error); // Only show binding errors, not warnings
#endif

		var app = builder.Build();

		// Initialize database in background - RECREATE if schema changed
		Task.Run(async () =>
		{
			try
			{
				using (var scope = app.Services.CreateScope())
				{
					var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
					
					// For development: Delete and recreate database when schema changes
					// Comment out these lines in production!
					await dbContext.Database.EnsureDeletedAsync();
					await dbContext.Database.EnsureCreatedAsync();
				}
			}
			catch (Exception)
			{
				// Database initialization failed - will be handled on first access
			}
		});

		// Optionally set default API key for development/testing
		// Users can still change it via Settings page
#if DEBUG
		Task.Run(async () =>
		{
			try
			{
				// Set default API key if none exists (for development convenience)
				// Remove or comment out in production
				await settingsService.SetDefaultApiKeyIfNotExistsAsync("AIzaSyDk4sifW4idrGAJW7emWFS23ziDKcW6X4k");
			}
			catch (Exception)
			{
				// API key initialization failed - user can set it manually via Settings
			}
		});
#endif

		return app;
	}
}
