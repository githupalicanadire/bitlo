using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using Shopping.Web.Services;

namespace Shopping.Web.Pages;

public class RegisterModel : PageModel
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<RegisterModel> _logger;

    [BindProperty]
    public RegisterInputModel RegisterData { get; set; } = default!;

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? ErrorMessage { get; set; }
    public IEnumerable<string>? ErrorMessages { get; set; }
    public string? SuccessMessage { get; set; }

    public RegisterModel(
        IAuthenticationService authenticationService,
        ILogger<RegisterModel> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        // Check if user is already authenticated
        if (await _authenticationService.IsAuthenticatedAsync())
        {
            return LocalRedirect(returnUrl ?? "/");
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
            _logger.LogInformation("Registration attempt for user: {Email}", RegisterData.Email);

            var result = await _authenticationService.RegisterAsync(
                RegisterData.FirstName,
                RegisterData.LastName,
                RegisterData.Email,
                RegisterData.Password,
                RegisterData.ConfirmPassword);

            if (result.Success)
            {
                _logger.LogInformation("Registration and auto-login successful for user: {Email}", RegisterData.Email);

                // User is automatically logged in after registration
                var redirectUrl = !string.IsNullOrEmpty(ReturnUrl) ? ReturnUrl : "/";
                return LocalRedirect(redirectUrl);
            }
            else
            {
                ErrorMessage = result.ErrorMessage ?? "Registration failed. Please try again.";
                ErrorMessages = result.Errors;

                _logger.LogWarning("Registration failed for user: {Email} - {Error}", RegisterData.Email, ErrorMessage);
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during registration for user: {Email}", RegisterData.Email);
            ErrorMessage = "An unexpected error occurred. Please try again.";
            return Page();
        }
    }
}

public class RegisterInputModel
{
    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = default!;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = default!;

    [Required]
    [EmailAddress]
    [Display(Name = "Email")]
    public string Email { get; set; } = default!;

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = default!;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = default!;
}
