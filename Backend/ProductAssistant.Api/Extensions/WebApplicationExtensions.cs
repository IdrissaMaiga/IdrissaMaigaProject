using Microsoft.EntityFrameworkCore;
using ProductAssistant.Core.Data;

namespace ProductAssistant.Api.Extensions;

/// <summary>
/// Extension methods for WebApplication configuration
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the HTTP request pipeline for Product Assistant API services
    /// </summary>
    public static WebApplication UseProductAssistantPipeline(
        this WebApplication app,
        bool enableSwagger = true,
        bool enableHttpsRedirection = true,
        string corsPolicyName = "AllowAll")
    {
        // Configure the HTTP request pipeline
        if (enableSwagger && app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Only use HTTPS redirection if HTTPS is configured
        if (enableHttpsRedirection && 
            app.Configuration["ASPNETCORE_URLS"]?.Contains("https") == true)
        {
            app.UseHttpsRedirection();
        }
        
        app.UseCors(corsPolicyName);
        app.UseAuthorization();
        app.MapControllers();
        
        // Health checks
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready");
        
        return app;
    }

    /// <summary>
    /// Ensures database is created and tests connection
    /// </summary>
    public static async Task<WebApplication> EnsureDatabaseCreatedAsync(
        this WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                // Try to get DbContextFactory first (preferred)
                var dbContextFactory = scope.ServiceProvider
                    .GetService<IDbContextFactory<AppDbContext>>();
                
                if (dbContextFactory != null)
                {
                    await using var dbContext = await dbContextFactory.CreateDbContextAsync();
                    await dbContext.Database.EnsureCreatedAsync();
                    
                    if (dbContext.Database.CanConnect())
                    {
                        Console.WriteLine("SQLite database connection established successfully");
                    }
                    else
                    {
                        Console.WriteLine("Warning: SQLite database connection not established");
                    }
                }
                else
                {
                    // Fallback to direct DbContext if factory is not available
                    var dbContext = scope.ServiceProvider.GetService<AppDbContext>();
                    if (dbContext != null)
                    {
                        await dbContext.Database.EnsureCreatedAsync();
                        
                        if (dbContext.Database.CanConnect())
                        {
                            Console.WriteLine("SQLite database connection established successfully");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not initialize database: {ex.Message}");
            }
        }
        
        return app;
    }
}

