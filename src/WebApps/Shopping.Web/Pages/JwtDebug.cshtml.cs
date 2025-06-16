using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shopping.Web.Services;
using System.IdentityModel.Tokens.Jwt;

namespace Shopping.Web.Pages;

public class JwtDebugModel : PageModel
{
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserService _userService;
    private readonly ILogger<JwtDebugModel> _logger;

    public bool IsAuthenticated { get; set; }
    public bool HasJwtToken { get; set; }
    public bool IsTokenValid { get; set; }
    public string? JwtToken { get; set; }
    public DateTime? TokenExpiration { get; set; }
    public UserInfo? UserInfo { get; set; }
    public Dictionary<string, string>? TokenClaims { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public JwtDebugModel(
        IJwtTokenService jwtTokenService,
        IAuthenticationService authenticationService,
        IUserService userService,
        ILogger<JwtDebugModel> logger)
    {
        _jwtTokenService = jwtTokenService;
        _authenticationService = authenticationService;
        _userService = userService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        await LoadTokenInformationAsync();
    }

    public async Task<IActionResult> OnPostRefreshTokenAsync()
    {
        try
        {
            var success = await _jwtTokenService.RefreshTokenAsync();
            if (success)
            {
                SuccessMessage = "Token refreshed successfully!";
                _logger.LogInformation("JWT token refreshed via debug page");
            }
            else
            {
                ErrorMessage = "Failed to refresh token. Please login again.";
                _logger.LogWarning("JWT token refresh failed via debug page");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error refreshing token: " + ex.Message;
            _logger.LogError(ex, "Exception during token refresh via debug page");
        }

        await LoadTokenInformationAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostValidateTokenAsync()
    {
        try
        {
            var isValid = await _jwtTokenService.ValidateTokenAsync();
            if (isValid)
            {
                SuccessMessage = "Token is valid!";
                _logger.LogInformation("JWT token validation successful via debug page");
            }
            else
            {
                ErrorMessage = "Token is invalid or expired.";
                _logger.LogWarning("JWT token validation failed via debug page");
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error validating token: " + ex.Message;
            _logger.LogError(ex, "Exception during token validation via debug page");
        }

        await LoadTokenInformationAsync();
        return Page();
    }

    private async Task LoadTokenInformationAsync()
    {
        try
        {
            // Check if user is authenticated (either JWT or OIDC)
            IsAuthenticated = await _authenticationService.IsAuthenticatedAsync() || 
                             (User.Identity?.IsAuthenticated == true);

            if (!IsAuthenticated)
                return;

            // Get JWT token information
            JwtToken = await _jwtTokenService.GetAccessTokenAsync();
            HasJwtToken = !string.IsNullOrEmpty(JwtToken);

            if (HasJwtToken)
            {
                IsTokenValid = !_jwtTokenService.IsTokenExpired(JwtToken!);
                TokenExpiration = _jwtTokenService.GetTokenExpiration(JwtToken!);

                // Parse token claims
                TokenClaims = ParseTokenClaims(JwtToken!);
            }

            // Get user information
            UserInfo = await _authenticationService.GetCurrentUserAsync();

            // If no JWT user info, get from OIDC claims
            if (UserInfo == null && User.Identity?.IsAuthenticated == true)
            {
                UserInfo = new UserInfo
                {
                    Id = User.FindFirst("sub")?.Value ?? "",
                    Email = User.FindFirst("email")?.Value ?? "",
                    Name = User.FindFirst("name")?.Value ?? "",
                    FirstName = User.FindFirst("given_name")?.Value ?? "",
                    LastName = User.FindFirst("family_name")?.Value ?? "",
                    EmailVerified = bool.TryParse(User.FindFirst("email_verified")?.Value, out var verified) && verified
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading token information for debug page");
            ErrorMessage = "Error loading token information: " + ex.Message;
        }
    }

    private Dictionary<string, string> ParseTokenClaims(string token)
    {
        var claims = new Dictionary<string, string>();
        
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            foreach (var claim in jsonToken.Claims)
            {
                var key = claim.Type;
                var value = claim.Value;

                // Make claim types more readable
                key = key switch
                {
                    "sub" => "Subject (User ID)",
                    "email" => "Email",
                    "name" => "Full Name",
                    "given_name" => "First Name",
                    "family_name" => "Last Name",
                    "iat" => "Issued At",
                    "exp" => "Expires At",
                    "jti" => "JWT ID",
                    "aud" => "Audience",
                    "iss" => "Issuer",
                    "scope" => "Scopes",
                    "client_id" => "Client ID",
                    _ => key
                };

                // Format timestamp values
                if (claim.Type is "iat" or "exp")
                {
                    if (long.TryParse(value, out var timestamp))
                    {
                        var dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp);
                        value = $"{value} ({dateTime:yyyy-MM-dd HH:mm:ss} UTC)";
                    }
                }

                claims[key] = value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JWT token claims");
            claims["Error"] = "Failed to parse token claims";
        }

        return claims;
    }
}
