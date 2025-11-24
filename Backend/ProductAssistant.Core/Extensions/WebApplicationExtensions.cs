using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ProductAssistant.Core.Extensions;

/// <summary>
/// Extension methods for WebApplication pipeline configuration
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures the HTTP request pipeline for Product Assistant services
    /// Note: Swagger should be configured in the consuming project (API, AIService, etc.)
    /// </summary>
    public static WebApplication UseProductAssistantPipeline(
        this WebApplication app,
        IHostEnvironment? environment = null,
        IConfiguration? configuration = null,
        bool enableHttpsRedirection = true,
        string corsPolicyName = "AllowAll")
    {
        var env = environment ?? app.Environment;
        
        // Only use HTTPS redirection if HTTPS is configured
        if (enableHttpsRedirection && 
            (configuration?["ASPNETCORE_URLS"]?.Contains("https") == true ||
             app.Configuration["ASPNETCORE_URLS"]?.Contains("https") == true))
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
}

