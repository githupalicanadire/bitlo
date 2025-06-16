namespace Shopping.Web.Services;

public interface IJwtTokenService
{
    Task<string?> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task SetTokensAsync(string accessToken, string refreshToken);
    Task<bool> RefreshTokenAsync();
    Task<bool> ValidateTokenAsync();
    Task ClearTokensAsync();
    bool IsTokenExpired(string token);
    DateTime? GetTokenExpiration(string token);
    string? GetUserIdFromToken(string token);
    string? GetUserEmailFromToken(string token);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;

    private const string ACCESS_TOKEN_KEY = "ToyShop.AccessToken";
    private const string REFRESH_TOKEN_KEY = "ToyShop.RefreshToken";

    public JwtTokenService(
        IHttpContextAccessor httpContextAccessor,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<JwtTokenService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        // Try cookies first
        if (context.Request.Cookies.TryGetValue(ACCESS_TOKEN_KEY, out var cookieToken))
        {
            if (!IsTokenExpired(cookieToken))
                return cookieToken;
            
            // Token expired, try to refresh
            if (await RefreshTokenAsync())
                return context.Request.Cookies[ACCESS_TOKEN_KEY];
        }

        return null;
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return null;

        context.Request.Cookies.TryGetValue(REFRESH_TOKEN_KEY, out var refreshToken);
        return refreshToken;
    }

    public async Task SetTokensAsync(string accessToken, string refreshToken)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = GetTokenExpiration(accessToken)?.AddMinutes(5) // Add buffer time
        };

        context.Response.Cookies.Append(ACCESS_TOKEN_KEY, accessToken, cookieOptions);
        
        var refreshCookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30) // Refresh token lasts longer
        };

        context.Response.Cookies.Append(REFRESH_TOKEN_KEY, refreshToken, refreshCookieOptions);

        _logger.LogDebug("Tokens stored in cookies");
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            var refreshToken = await GetRefreshTokenAsync();

            if (string.IsNullOrEmpty(accessToken) || string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Cannot refresh token - missing access or refresh token");
                return false;
            }

            var httpClient = _httpClientFactory.CreateClient();
            var identityUrl = _configuration["IdentityServer:BaseUrl"];

            var requestData = new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

            var response = await httpClient.PostAsJsonAsync($"{identityUrl}/api/auth/refresh-token", requestData);

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = await response.Content.ReadFromJsonAsync<dynamic>();
                var newAccessToken = tokenResponse?.access_token?.ToString();
                var newRefreshToken = tokenResponse?.refresh_token?.ToString();

                if (!string.IsNullOrEmpty(newAccessToken) && !string.IsNullOrEmpty(newRefreshToken))
                {
                    await SetTokensAsync(newAccessToken, newRefreshToken);
                    _logger.LogInformation("Token refreshed successfully");
                    return true;
                }
            }

            _logger.LogWarning("Failed to refresh token - HTTP {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return false;
        }
    }

    public async Task<bool> ValidateTokenAsync()
    {
        try
        {
            var accessToken = await GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
                return false;

            var httpClient = _httpClientFactory.CreateClient();
            var identityUrl = _configuration["IdentityServer:BaseUrl"];

            var requestData = new { AccessToken = accessToken };
            var response = await httpClient.PostAsJsonAsync($"{identityUrl}/api/auth/validate-token", requestData);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return false;
        }
    }

    public async Task ClearTokensAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null) return;

        context.Response.Cookies.Delete(ACCESS_TOKEN_KEY);
        context.Response.Cookies.Delete(REFRESH_TOKEN_KEY);

        _logger.LogDebug("Tokens cleared from cookies");
    }

    public bool IsTokenExpired(string token)
    {
        var expiration = GetTokenExpiration(token);
        return expiration.HasValue && expiration.Value <= DateTime.UtcNow.AddMinutes(5); // 5 minute buffer
    }

    public DateTime? GetTokenExpiration(string token)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            return jsonToken.ValidTo;
        }
        catch
        {
            return null;
        }
    }

    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            return jsonToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
        }
        catch
        {
            return null;
        }
    }

    public string? GetUserEmailFromToken(string token)
    {
        try
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);
            return jsonToken.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
        }
        catch
        {
            return null;
        }
    }
}
