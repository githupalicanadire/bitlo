namespace Shopping.Web.Services;

public interface IUserService
{
    string? GetCurrentUserName();
    string? GetCurrentUserId();
    string? GetCurrentUserEmail();
    bool IsAuthenticated();
    Task<UserInfo?> GetCurrentUserInfoAsync();
}

public class UserService : IUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IAuthenticationService _authenticationService;

    public UserService(
        IHttpContextAccessor httpContextAccessor,
        IJwtTokenService jwtTokenService,
        IAuthenticationService authenticationService)
    {
        _httpContextAccessor = httpContextAccessor;
        _jwtTokenService = jwtTokenService;
        _authenticationService = authenticationService;
    }

    public string? GetCurrentUserName()
    {
        // First try JWT token
        var accessToken = _jwtTokenService.GetAccessTokenAsync().Result;
        if (!string.IsNullOrEmpty(accessToken))
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            try
            {
                var jsonToken = handler.ReadJwtToken(accessToken);
                return jsonToken.Claims.FirstOrDefault(x => x.Type == "name")?.Value ??
                       jsonToken.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
            }
            catch { }
        }

        // Fallback to OIDC claims
        var context = _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst("name")?.Value ??
                   context.User.FindFirst("email")?.Value ??
                   context.User.Identity.Name;
        }
        return null;
    }

    public string? GetCurrentUserId()
    {
        // First try JWT token
        var accessToken = _jwtTokenService.GetAccessTokenAsync().Result;
        if (!string.IsNullOrEmpty(accessToken))
        {
            return _jwtTokenService.GetUserIdFromToken(accessToken);
        }

        // Fallback to OIDC claims
        var context = _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst("sub")?.Value ??
                   context.User.FindFirst("id")?.Value;
        }
        return null;
    }

    public string? GetCurrentUserEmail()
    {
        // First try JWT token
        var accessToken = _jwtTokenService.GetAccessTokenAsync().Result;
        if (!string.IsNullOrEmpty(accessToken))
        {
            return _jwtTokenService.GetUserEmailFromToken(accessToken);
        }

        // Fallback to OIDC claims
        var context = _httpContextAccessor.HttpContext;
        if (context?.User?.Identity?.IsAuthenticated == true)
        {
            return context.User.FindFirst("email")?.Value;
        }
        return null;
    }

    public bool IsAuthenticated()
    {
        // Check JWT token first
        var accessToken = _jwtTokenService.GetAccessTokenAsync().Result;
        if (!string.IsNullOrEmpty(accessToken) && !_jwtTokenService.IsTokenExpired(accessToken))
        {
            return true;
        }

        // Fallback to OIDC authentication
        var context = _httpContextAccessor.HttpContext;
        return context?.User?.Identity?.IsAuthenticated == true;
    }

    public async Task<UserInfo?> GetCurrentUserInfoAsync()
    {
        try
        {
            return await _authenticationService.GetCurrentUserAsync();
        }
        catch
        {
            return null;
        }
    }
}
