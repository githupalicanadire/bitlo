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

    [HttpGet("users")]
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
