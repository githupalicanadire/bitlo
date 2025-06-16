using Identity.API.Models;
using Identity.API.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IdentityServer4.Services;
using IdentityServer4.Models;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Identity.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AuthController> _logger;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AuthController> logger,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return BadRequest(new { error = "validation_failed", error_description = "Validation failed", errors = errors });
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("Registration attempt with existing email: {Email}", model.Email);
            return BadRequest(new { error = "user_exists", error_description = "A user with this email already exists" });
        }

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            EmailConfirmed = true, // For development
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            _logger.LogInformation("User created successfully: {Email}", model.Email);

            // Auto-login after successful registration
            var loginRequest = new LoginViewModel
            {
                Email = model.Email,
                Password = model.Password,
                RememberMe = false
            };

            return await Login(loginRequest);
        }

        var errorDescriptions = result.Errors.Select(e => e.Description);
        _logger.LogWarning("User registration failed for {Email}: {Errors}", model.Email, string.Join(", ", errorDescriptions));

        return BadRequest(new
        {
            error = "registration_failed",
            error_description = "Registration failed",
            errors = errorDescriptions
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            _logger.LogWarning("Login attempt with invalid email: {Email}", model.Email);
            return BadRequest(new { error = "invalid_credentials", error_description = "Invalid email or password" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

        if (result.Succeeded)
        {
            _logger.LogInformation("User logged in successfully: {Email}", model.Email);

            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim("given_name", user.FirstName),
                new Claim("family_name", user.LastName),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Create Identity Server token request
            var tokenRequest = new TokenCreationRequest
            {
                Subject = new ClaimsIdentity(claims),
                ValidatedRequest = new ValidatedRequest
                {
                    Client = new IdentityServer4.Models.Client
                    {
                        ClientId = "shopping.web",
                        AllowedScopes = { "openid", "profile", "email", "shopping.web", "catalog.api", "basket.api", "ordering.api" }
                    },
                    Options = new IdentityServerOptions()
                }
            };

            // Generate tokens using Identity Server
            var accessToken = await _tokenService.CreateAccessTokenAsync(tokenRequest);
            var refreshToken = await _tokenService.CreateRefreshTokenAsync(new RefreshTokenCreationRequest
            {
                AccessToken = accessToken,
                Client = tokenRequest.ValidatedRequest.Client,
                Subject = tokenRequest.Subject
            });

            var tokenResponse = new
            {
                access_token = await _tokenService.CreateSecurityTokenAsync(accessToken),
                refresh_token = refreshToken.CreationTime.ToString(),
                token_type = "Bearer",
                expires_in = accessToken.Lifetime,
                scope = string.Join(" ", accessToken.Scopes),
                user_info = new
                {
                    id = user.Id,
                    email = user.Email,
                    name = user.FullName,
                    first_name = user.FirstName,
                    last_name = user.LastName,
                    email_verified = user.EmailConfirmed,
                    created_date = user.CreatedDate
                }
            };

            return Ok(tokenResponse);
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User account locked out: {Email}", model.Email);
            return BadRequest(new { error = "account_locked", error_description = "Account is temporarily locked" });
        }

        if (result.IsNotAllowed)
        {
            _logger.LogWarning("User login not allowed: {Email}", model.Email);
            return BadRequest(new { error = "login_not_allowed", error_description = "Login not allowed" });
        }

        _logger.LogWarning("Invalid password for user: {Email}", model.Email);
        return BadRequest(new { error = "invalid_credentials", error_description = "Invalid email or password" });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest model)
    {
        if (string.IsNullOrEmpty(model.RefreshToken))
            return BadRequest(new { error = "invalid_request", error_description = "Refresh token is required" });

        try
        {
            // In a real implementation, you would validate the refresh token against stored data
            // For now, we'll decode the access token to get user info and issue a new token

            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(model.AccessToken);
            var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return BadRequest(new { error = "invalid_token", error_description = "Invalid access token" });

            var user = await _userManager.FindByIdAsync(userIdClaim);
            if (user == null)
                return BadRequest(new { error = "user_not_found", error_description = "User not found" });

            // Create new token (similar to login)
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Name, user.FullName),
                new Claim("given_name", user.FirstName),
                new Claim("family_name", user.LastName),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var tokenRequest = new TokenCreationRequest
            {
                Subject = new ClaimsIdentity(claims),
                ValidatedRequest = new ValidatedRequest
                {
                    Client = new IdentityServer4.Models.Client
                    {
                        ClientId = "shopping.web",
                        AllowedScopes = { "openid", "profile", "email", "shopping.web", "catalog.api", "basket.api", "ordering.api" }
                    },
                    Options = new IdentityServerOptions()
                }
            };

            var accessToken = await _tokenService.CreateAccessTokenAsync(tokenRequest);

            _logger.LogInformation("Token refreshed for user: {Email}", user.Email);

            return Ok(new
            {
                access_token = await _tokenService.CreateSecurityTokenAsync(accessToken),
                refresh_token = model.RefreshToken, // In production, generate new refresh token
                token_type = "Bearer",
                expires_in = accessToken.Lifetime,
                scope = string.Join(" ", accessToken.Scopes)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return BadRequest(new { error = "invalid_token", error_description = "Token refresh failed" });
        }
    }

    [HttpPost("validate-token")]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest model)
    {
        if (string.IsNullOrEmpty(model.AccessToken))
            return BadRequest(new { error = "invalid_request", error_description = "Access token is required" });

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(model.AccessToken);

            // Check if token is expired
            if (jsonToken.ValidTo < DateTime.UtcNow)
                return BadRequest(new { error = "token_expired", error_description = "Token has expired" });

            var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return BadRequest(new { error = "invalid_token", error_description = "Invalid token format" });

            var user = await _userManager.FindByIdAsync(userIdClaim);
            if (user == null)
                return BadRequest(new { error = "user_not_found", error_description = "User not found" });

            return Ok(new
            {
                valid = true,
                user_info = new
                {
                    id = user.Id,
                    email = user.Email,
                    name = user.FullName,
                    first_name = user.FirstName,
                    last_name = user.LastName
                },
                token_info = new
                {
                    expires_at = jsonToken.ValidTo,
                    issued_at = jsonToken.ValidFrom,
                    scopes = jsonToken.Claims.Where(c => c.Type == "scope").Select(c => c.Value),
                    client_id = jsonToken.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return BadRequest(new { error = "invalid_token", error_description = "Token validation failed" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var userEmail = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

        // In a real implementation, you would invalidate the refresh token
        // and add the access token to a blacklist

        _logger.LogInformation("User logged out: {Email} (ID: {UserId})", userEmail, userId);

        return Ok(new { message = "Logout successful" });
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (string.IsNullOrEmpty(userId))
            return BadRequest(new { error = "invalid_token", error_description = "User ID not found in token" });

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(new { error = "user_not_found", error_description = "User not found" });

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            name = user.FullName,
            first_name = user.FirstName,
            last_name = user.LastName,
            email_verified = user.EmailConfirmed,
            is_active = user.IsActive,
            created_date = user.CreatedDate
        });
    }

    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.FullName,
                u.CreatedDate,
                u.IsActive
            })
            .ToListAsync();

        return Ok(users);
    }
}
