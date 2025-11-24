using Microsoft.AspNetCore.Http;
using ProductAssistant.Core.Services;
using System.Security.Claims;

namespace ProductAssistant.Api.Middleware;

/// <summary>
/// Middleware for JWT token authentication
/// Validates tokens from Authorization header and sets user context
/// </summary>
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
        // Skip authentication for public endpoints
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
        if (path.StartsWith("/health") || 
            path.StartsWith("/swagger") || 
            path.StartsWith("/api/auth/login") ||
            path.StartsWith("/api/auth/register"))
        {
            await _next(context);
            return;
        }

        // Try to get token from Authorization header
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            
            if (tokenService.ValidateToken(token, out var userId, out var username))
            {
                // Set user claims for authorization
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId ?? ""),
                    new Claim(ClaimTypes.Name, username ?? "")
                };
                
                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
                _logger.LogDebug("User authenticated: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Invalid token provided");
            }
        }

        await _next(context);
    }
}

