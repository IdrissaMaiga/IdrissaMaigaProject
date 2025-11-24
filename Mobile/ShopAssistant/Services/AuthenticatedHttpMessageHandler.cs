using ProductAssistant.Core.Services;

namespace ShopAssistant.Services;

public class AuthenticatedHttpMessageHandler : DelegatingHandler
{
    private readonly IAuthService _authService;

    public AuthenticatedHttpMessageHandler(IAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get the token from auth service
        var token = await _authService.GetAccessTokenAsync();
        
        if (!string.IsNullOrEmpty(token))
        {
            // Add Authorization header
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
