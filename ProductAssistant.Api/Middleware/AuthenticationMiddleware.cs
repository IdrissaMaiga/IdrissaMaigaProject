using ProductAssistant.Core.Services;
using System.Text.Json;

namespace ProductAssistant.Api.Middleware;

public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        // Allow public endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        if (path.StartsWith("/api/auth/login") || 
            path.StartsWith("/api/auth/register") ||
            path.StartsWith("/api/health") ||
            path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        // Check for Authorization header
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            _logger.LogWarning("Unauthorized request to {Path} - No Authorization header", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized - Missing authentication token" });
            return;
        }

        // Extract token from "Bearer <token>" format
        var token = authHeader.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();
        
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Unauthorized request to {Path} - Empty token", path);
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized - Invalid token format" });
            return;
        }

        // Validate token
        if (!tokenService.ValidateToken(token, out var userId, out var username))
        {
            _logger.LogWarning("Unauthorized request to {Path} - Invalid token for user {UserId}", path, userId ?? "unknown");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Unauthorized - Invalid or expired token" });
            return;
        }

        // Store user info in context for controllers to use
        context.Items["UserId"] = userId;
        context.Items["Username"] = username;

        await _next(context);
    }
}



