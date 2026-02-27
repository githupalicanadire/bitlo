using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Shopping.Web.Pages;

public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    [BindProperty]
    public LoginInputModel LoginData { get; set; } = default!;

    public LoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        // Check if we're in development bypass mode
        var bypassAuth = _configuration.GetValue<bool>("DevelopmentMode:BypassAuthentication");

        if (bypassAuth)
        {
            // In development mode, redirect directly to home
            return LocalRedirect(returnUrl ?? "/");
        }

        // If user is already authenticated, redirect to return URL or home
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        // Redirect to Identity Server with return URL
        var redirectUri = !string.IsNullOrEmpty(returnUrl) ? returnUrl : Url.Page("/Index");

        return Challenge(new AuthenticationProperties
        {
            RedirectUri = redirectUri
        }, "oidc");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // If user is already authenticated, redirect to home
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Index");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Redirect to Identity Server for authentication
        return Challenge(new AuthenticationProperties
        {
            RedirectUri = Url.Page("/Index")
        }, "oidc");
    }
}

public class LoginInputModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = default!;

    public bool RememberMe { get; set; }
}
