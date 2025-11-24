using CommunityToolkit.Maui;
using Microsoft.EntityFrameworkCore;
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

		// SQLite Database for MAUI app - Use Factory pattern for Singleton services
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "shopassistant.db");
		builder.Services.AddDbContextFactory<AppDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"));
		
		// Also add regular DbContext for views that need it directly
		builder.Services.AddDbContext<AppDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"), ServiceLifetime.Scoped);

		// Configure API URLs - Platform-aware (Android emulator uses 10.0.2.2)
		var apiBaseUrl = $"{ServiceUrlHelper.GetApiBaseUrl()}/api";
		var aiServiceUrl = $"{ServiceUrlHelper.GetAIServiceUrl()}/api";
		var scrapingServiceUrl = $"{ServiceUrlHelper.GetScrapingServiceUrl()}/api";

		// Register HTTP clients with base addresses
		builder.Services.AddHttpClient("ApiService", client =>
		{
			client.BaseAddress = new Uri(apiBaseUrl);
			client.Timeout = TimeSpan.FromSeconds(30);
		});

		builder.Services.AddHttpClient("AIService", client =>
		{
			client.BaseAddress = new Uri(aiServiceUrl);
			client.Timeout = TimeSpan.FromSeconds(60);
		});

		builder.Services.AddHttpClient("ScrapingService", client =>
		{
			client.BaseAddress = new Uri(scrapingServiceUrl);
			client.Timeout = TimeSpan.FromSeconds(30);
		});

		builder.Services.AddHttpClient<IArukeresoScrapingService, ArukeresoScrapingService>(client =>
		{
			client.BaseAddress = new Uri($"{scrapingServiceUrl}/scraping");
			client.Timeout = TimeSpan.FromSeconds(30);
		});
		
		// Register IAuthService first so it can be injected into the handler
		// Note: Auth0Service stores tokens in memory/Preferences, so GetAccessTokenAsync() doesn't make HTTP calls
		builder.Services.AddHttpClient<IAuthService, Auth0Service>(client =>
		{
			client.BaseAddress = new Uri(apiBaseUrl);
			client.Timeout = TimeSpan.FromSeconds(30);
		});
		
		// Register the authenticated message handler as transient
		builder.Services.AddTransient<AuthenticatedHttpMessageHandler>();
		
		// ProductService needs authentication - use AuthenticatedHttpMessageHandler
		builder.Services.AddHttpClient<IProductService, ProductService>(client =>
		{
			client.BaseAddress = new Uri($"{apiBaseUrl}/products");
			client.Timeout = TimeSpan.FromSeconds(30);
		})
		.AddHttpMessageHandler<AuthenticatedHttpMessageHandler>();
		builder.Services.AddHttpClient<ILLMService, GeminiService>(client =>
		{
			client.Timeout = TimeSpan.FromMinutes(2);
		});

		// Register services as Singleton
		builder.Services.AddSingleton<SettingsService>();
		builder.Services.AddSingleton<IConversationMemoryService, ConversationMemoryService>();
		builder.Services.AddSingleton<IConversationalAIService, ConversationalAIService>();
		builder.Services.AddSingleton<INetworkService, NetworkService>();
		builder.Services.AddSingleton<IGeolocationService, GeolocationService>();

		// Register ViewModels as Transient (new instance per navigation - MAUI best practice)
		builder.Services.AddTransient<ProductsViewModel>();
		builder.Services.AddTransient<ProductDetailViewModel>();
		builder.Services.AddTransient<ChatViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();
		builder.Services.AddTransient<LoginViewModel>();

		// Register Views as Transient (new instance per navigation)
		builder.Services.AddTransient<ProductsPage>();
		builder.Services.AddTransient<ProductDetailPage>();
		builder.Services.AddTransient<ChatPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<SettingsPage>();
		
		// Register AppShell as Singleton
		builder.Services.AddSingleton<AppShell>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Initialize database in background - RECREATE if schema changed
		Task.Run(async () =>
		{
			try
			{
				System.Diagnostics.Debug.WriteLine("=== Database initialization starting ===");
				using (var scope = app.Services.CreateScope())
				{
					var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
					
					// For development: Delete and recreate database when schema changes
					// Comment out these lines in production!
					System.Diagnostics.Debug.WriteLine("=== Deleting old database ===");
					await dbContext.Database.EnsureDeletedAsync();
					
					System.Diagnostics.Debug.WriteLine("=== Creating new database ===");
					await dbContext.Database.EnsureCreatedAsync();
					
					System.Diagnostics.Debug.WriteLine("✅ Database initialized successfully");
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"❌ Database initialization error: {ex.GetType().Name}");
				System.Diagnostics.Debug.WriteLine($"❌ Message: {ex.Message}");
				System.Diagnostics.Debug.WriteLine($"❌ Stack: {ex.StackTrace}");
			}
		});

		return app;
	}
}
