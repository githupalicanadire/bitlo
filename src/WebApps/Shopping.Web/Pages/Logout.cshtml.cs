using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Shopping.Web.Services;

namespace Shopping.Web.Pages;

public class LogoutModel : PageModel
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(
        IAuthenticationService authenticationService,
        ILogger<LogoutModel> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return await OnPostAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            // Logout from JWT authentication first
            await _authenticationService.LogoutAsync();
            _logger.LogInformation("JWT logout completed");

            // Also sign out from OIDC if user was logged in via OIDC
            if (User.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("Signing out from OIDC");
                return SignOut(new AuthenticationProperties
                {
                    RedirectUri = Url.Page("/Index")
                }, "Cookies", "oidc");
            }

            // Direct redirect if only JWT was used
            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            // Even if there's an error, redirect to home
            return RedirectToPage("/Index");
        }
    }
}
