using ProductAssistant.Api.Extensions;
using ProductAssistant.Api.Middleware;
using ProductAssistant.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Ignore circular references (e.g., Product.Messages -> ConversationMessage -> Product)
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // Use camelCase for JSON property names
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Support UTF-8 encoding for special characters
        options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

// Add Product Assistant services
builder.Services.AddProductAssistantSwagger(); // From API.Extensions (includes SwaggerGen)
builder.Services.AddProductAssistantDatabase(builder.Configuration);
ProductAssistant.Core.Extensions.ServiceCollectionExtensions.AddProductAssistantAuthentication(builder.Services, builder.Configuration);
builder.Services.AddProductAssistantCors();
builder.Services.AddProductAssistantHttpClients(builder.Configuration);
builder.Services.AddProductAssistantApiServices(builder.Configuration);
// Register API-specific ProductService implementation
builder.Services.AddScoped<ProductAssistant.Core.Services.IProductService, ProductAssistant.Api.Services.ProductService>();
ProductAssistant.Core.Extensions.ServiceCollectionExtensions.AddProductAssistantHealthChecks(builder.Services);

var app = builder.Build();

// Configure the HTTP request pipeline (using API-specific extension to avoid ambiguity)
ProductAssistant.Api.Extensions.WebApplicationExtensions.UseProductAssistantPipeline(app);

// Add authentication middleware BEFORE authorization
app.UseMiddleware<AuthenticationMiddleware>();

// Ensure database is created and test connection
await app.EnsureDatabaseCreatedAsync();

app.Run();

