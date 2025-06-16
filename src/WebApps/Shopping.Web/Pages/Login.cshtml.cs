using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Shopping.Web.Services;

namespace Shopping.Web.Pages;

public class LoginModel : PageModel
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserService _userService;
    private readonly ILogger<LoginModel> _logger;

    [BindProperty]
    public LoginInputModel LoginData { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }

    public LoginModel(
        IAuthenticationService authenticationService,
        IUserService userService,
        ILogger<LoginModel> logger)
    {
        _authenticationService = authenticationService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        // Check if user is already authenticated (JWT)
        if (await _authenticationService.IsAuthenticatedAsync())
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        // Also check OIDC authentication for backward compatibility
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(returnUrl ?? "/");
        }

        // Clear any previous login message cookie
        if (Request.Cookies.ContainsKey("LoginMessage"))
        {
            Response.Cookies.Delete("LoginMessage");
        }

        // Set user-friendly message based on return URL
        if (!string.IsNullOrEmpty(returnUrl))
        {
            if (returnUrl.Contains("/cart"))
                ViewData["LoginMessage"] = "Please sign in to view your shopping cart.";
            else if (returnUrl.Contains("/checkout"))
                ViewData["LoginMessage"] = "Please sign in to proceed with checkout.";
            else if (returnUrl.Contains("/order"))
                ViewData["LoginMessage"] = "Please sign in to view your orders.";
            else
                ViewData["LoginMessage"] = "Please sign in to continue.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            _logger.LogInformation("Login attempt for user: {Email}", LoginData.Email);

            var result = await _authenticationService.LoginAsync(
                LoginData.Email,
                LoginData.Password,
                LoginData.RememberMe);

            if (result.Success)
            {
                _logger.LogInformation("Login successful for user: {Email}", LoginData.Email);

                // Redirect to return URL or home
                var redirectUrl = !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : "/";
                return LocalRedirect(redirectUrl);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Login failed. Please check your credentials.";
                _logger.LogWarning("Login failed for user: {Email} - {Error}", LoginData.Email, ErrorMessage);
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during login for user: {Email}", LoginData.Email);
            ErrorMessage = "An unexpected error occurred. Please try again.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostOidcLoginAsync()
    {
        // Fallback to OIDC authentication if JWT login fails
        var redirectUri = !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : Url.Page("/Index");

        return Challenge(new AuthenticationProperties
        {
            RedirectUri = redirectUri
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
