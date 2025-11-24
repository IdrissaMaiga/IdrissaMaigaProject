using ProductAssistant.AIService.Extensions;
using ProductAssistant.Core.Extensions;
using ProductAssistant.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Product Assistant services
builder.Services.AddProductAssistantSwagger(); // From AIService.Extensions (includes SwaggerGen)
builder.Services.AddProductAssistantDatabase(builder.Configuration); // From AIService.Extensions
builder.Services.AddProductAssistantCors(); // From AIService.Extensions
builder.Services.AddProductAssistantHttpClients(builder.Configuration); // From AIService.Extensions

// Register IProductService - AI Service calls API Service via HTTP
// Tools need this to access user products
var apiServiceUrl = builder.Configuration["ServiceOptions:ApiServiceUrl"] ?? "http://api-service:8080";
builder.Services.AddHttpClient<IProductService, ProductAssistant.Core.Services.ProductService>(client =>
{
    client.BaseAddress = new Uri($"{apiServiceUrl}/api/products");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Use Core.Extensions method which registers all AI services and tools
// This registers tools, tool executor, and ConversationalAIService
// Note: IProductService is already registered above via HTTP client
ProductAssistant.Core.Extensions.ServiceCollectionExtensions.AddProductAssistantAIServices(builder.Services);
builder.Services.AddProductAssistantHealthChecks(); // From Core.Extensions

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseProductAssistantPipeline(app.Environment, app.Configuration);

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductAssistant.Core.Data.AppDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    
    if (dbContext.Database.CanConnect())
    {
        Console.WriteLine("SQLite database connection established successfully");
    }
}

app.Run();

