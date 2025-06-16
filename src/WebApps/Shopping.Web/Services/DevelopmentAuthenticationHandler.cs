using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Shopping.Web.Services;

public class DevelopmentAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
}

public class DevelopmentAuthenticationHandler : AuthenticationHandler<DevelopmentAuthenticationSchemeOptions>
{
    private readonly IConfiguration _configuration;

    public DevelopmentAuthenticationHandler(
        IOptionsMonitor<DevelopmentAuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        IConfiguration configuration)
        : base(options, logger, encoder, clock)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "dev-user-123"),
            new Claim(ClaimTypes.Name, _configuration["DevelopmentMode:MockUser:UserName"] ?? "admin@toyshop.com"),
            new Claim(ClaimTypes.Email, _configuration["DevelopmentMode:MockUser:Email"] ?? "admin@toyshop.com"),
            new Claim(ClaimTypes.GivenName, _configuration["DevelopmentMode:MockUser:FirstName"] ?? "Admin"),
            new Claim(ClaimTypes.Surname, _configuration["DevelopmentMode:MockUser:LastName"] ?? "User"),
            new Claim("scope", "openid"),
            new Claim("scope", "profile"),
            new Claim("scope", "email"),
            new Claim("scope", "shopping.web"),
            new Claim("scope", "catalog.api"),
            new Claim("scope", "basket.api"),
            new Claim("scope", "ordering.api")
        };

        var identity = new ClaimsIdentity(claims, "Development");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Development");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
