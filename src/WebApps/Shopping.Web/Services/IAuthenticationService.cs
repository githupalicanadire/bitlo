namespace Shopping.Web.Services;

public interface IAuthenticationService
{
    Task<AuthResult> LoginAsync(string email, string password, bool rememberMe = false);
    Task<AuthResult> RegisterAsync(string firstName, string lastName, string email, string password, string confirmPassword);
    Task<bool> LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<UserInfo?> GetCurrentUserAsync();
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IJwtTokenService _tokenService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IHttpClientFactory httpClientFactory,
        IJwtTokenService tokenService,
        IConfiguration configuration,
        ILogger<AuthenticationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _tokenService = tokenService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<AuthResult> LoginAsync(string email, string password, bool rememberMe = false)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var identityUrl = _configuration["IdentityServer:BaseUrl"];

            var loginRequest = new
            {
                Email = email,
                Password = password,
                RememberMe = rememberMe
            };

            _logger.LogInformation("Attempting login for user: {Email}", email);

            var response = await httpClient.PostAsJsonAsync($"{identityUrl}/api/auth/login", loginRequest);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                
                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    await _tokenService.SetTokensAsync(loginResponse.AccessToken, loginResponse.RefreshToken ?? "");
                    
                    _logger.LogInformation("Login successful for user: {Email}", email);
                    
                    return new AuthResult
                    {
                        Success = true,
                        AccessToken = loginResponse.AccessToken,
                        UserInfo = loginResponse.UserInfo
                    };
                }
            }

            var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            var errorMessage = errorResponse?.ErrorDescription ?? "Login failed";

            _logger.LogWarning("Login failed for user: {Email} - {Error}", email, errorMessage);

            return new AuthResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during login for user: {Email}", email);
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "An error occurred during login. Please try again."
            };
        }
    }

    public async Task<AuthResult> RegisterAsync(string firstName, string lastName, string email, string password, string confirmPassword)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var identityUrl = _configuration["IdentityServer:BaseUrl"];

            var registerRequest = new
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Password = password,
                ConfirmPassword = confirmPassword
            };

            _logger.LogInformation("Attempting registration for user: {Email}", email);

            var response = await httpClient.PostAsJsonAsync($"{identityUrl}/api/auth/register", registerRequest);

            if (response.IsSuccessStatusCode)
            {
                var registerResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                
                if (registerResponse != null && !string.IsNullOrEmpty(registerResponse.AccessToken))
                {
                    await _tokenService.SetTokensAsync(registerResponse.AccessToken, registerResponse.RefreshToken ?? "");
                    
                    _logger.LogInformation("Registration and auto-login successful for user: {Email}", email);
                    
                    return new AuthResult
                    {
                        Success = true,
                        AccessToken = registerResponse.AccessToken,
                        UserInfo = registerResponse.UserInfo
                    };
                }
            }

            var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            var errorMessage = errorResponse?.ErrorDescription ?? "Registration failed";

            _logger.LogWarning("Registration failed for user: {Email} - {Error}", email, errorMessage);

            return new AuthResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Errors = errorResponse?.Errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during registration for user: {Email}", email);
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "An error occurred during registration. Please try again."
            };
        }
    }

    public async Task<bool> LogoutAsync()
    {
        try
        {
            var accessToken = await _tokenService.GetAccessTokenAsync();
            
            if (!string.IsNullOrEmpty(accessToken))
            {
                var httpClient = _httpClientFactory.CreateClient();
                var identityUrl = _configuration["IdentityServer:BaseUrl"];

                httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                await httpClient.PostAsync($"{identityUrl}/api/auth/logout", null);
            }

            await _tokenService.ClearTokensAsync();
            _logger.LogInformation("User logged out successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            await _tokenService.ClearTokensAsync(); // Clear tokens anyway
            return false;
        }
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var accessToken = await _tokenService.GetAccessTokenAsync();
        return !string.IsNullOrEmpty(accessToken) && await _tokenService.ValidateTokenAsync();
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        try
        {
            var accessToken = await _tokenService.GetAccessTokenAsync();
            if (string.IsNullOrEmpty(accessToken))
                return null;

            var httpClient = _httpClientFactory.CreateClient();
            var identityUrl = _configuration["IdentityServer:BaseUrl"];

            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync($"{identityUrl}/api/auth/profile");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserInfo>();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return null;
        }
    }
}

// DTOs for API responses
public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public IEnumerable<string>? Errors { get; set; }
    public string? AccessToken { get; set; }
    public UserInfo? UserInfo { get; set; }
}

public class LoginResponse
{
    public string AccessToken { get; set; } = default!;
    public string? RefreshToken { get; set; }
    public string TokenType { get; set; } = default!;
    public int ExpiresIn { get; set; }
    public string? Scope { get; set; }
    public UserInfo UserInfo { get; set; } = default!;
}

public class ErrorResponse
{
    public string Error { get; set; } = default!;
    public string ErrorDescription { get; set; } = default!;
    public IEnumerable<string>? Errors { get; set; }
}

public class UserInfo
{
    public string Id { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public bool EmailVerified { get; set; }
    public DateTime CreatedDate { get; set; }
}
