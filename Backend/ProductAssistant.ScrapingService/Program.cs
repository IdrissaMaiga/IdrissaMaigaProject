using ProductAssistant.Core.Extensions;
using ProductAssistant.Core.Services;
using ProductAssistant.ScrapingService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Add Product Assistant services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProductAssistantCors();
builder.Services.AddProductAssistantScrapingService<DirectArukeresoScrapingService>();
builder.Services.AddProductAssistantHealthChecks(includeDatabase: false);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseProductAssistantPipeline(app.Environment, app.Configuration);

app.Run();

