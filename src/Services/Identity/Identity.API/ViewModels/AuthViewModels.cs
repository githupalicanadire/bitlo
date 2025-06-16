namespace Identity.API.ViewModels;

public class RefreshTokenRequest
{
    [Required]
    public string AccessToken { get; set; } = default!;

    [Required]
    public string RefreshToken { get; set; } = default!;
}

public class ValidateTokenRequest
{
    [Required]
    public string AccessToken { get; set; } = default!;
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

public class RegisterViewModel
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = default!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = default!;

    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = default!;
}
