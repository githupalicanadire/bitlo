using Microsoft.AspNetCore.Authentication;

namespace Shopping.Web.Services;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthenticatedHttpClientHandler> _logger;

    public AuthenticatedHttpClientHandler(
        IHttpContextAccessor httpContextAccessor,
        IJwtTokenService jwtTokenService,
        ILogger<AuthenticatedHttpClientHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            // Try to get JWT token from our token service
            var accessToken = await _jwtTokenService.GetAccessTokenAsync();

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                _logger.LogDebug("Added JWT Bearer token to request for {RequestUri}", request.RequestUri);
            }
            else
            {
                // Fallback to OIDC token if JWT is not available
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity?.IsAuthenticated == true)
                {
                    var oidcToken = await httpContext.GetTokenAsync("access_token");
                    if (!string.IsNullOrEmpty(oidcToken))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", oidcToken);
                        _logger.LogDebug("Added OIDC Bearer token to request for {RequestUri}", request.RequestUri);
                    }
                    else
                    {
                        _logger.LogWarning("No access token found for authenticated user");
                    }
                }
                else
                {
                    _logger.LogDebug("User not authenticated, skipping token attachment");
                }
            }

            var response = await base.SendAsync(request, cancellationToken);

            // Handle 401 Unauthorized - token might be expired
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("Received 401 Unauthorized, attempting token refresh");

                if (await _jwtTokenService.RefreshTokenAsync())
                {
                    var newToken = await _jwtTokenService.GetAccessTokenAsync();
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", newToken);
                        _logger.LogInformation("Token refreshed, retrying request");

                        // Retry the request with new token
                        response = await base.SendAsync(request, cancellationToken);
                    }
                }
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AuthenticatedHttpClientHandler");
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
