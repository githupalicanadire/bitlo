using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Shopping.Web.Pages;

public class LoginModel : PageModel
{
    [BindProperty]
    public LoginInputModel LoginData { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        // If user is already authenticated, redirect to return URL or home
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
