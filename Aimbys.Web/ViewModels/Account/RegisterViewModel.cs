using System.ComponentModel.DataAnnotations;

namespace Aimbys.Web.ViewModels.Account;

public class RegisterViewModel
{
    [Required, EmailAddress, Display(Name = "Email")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Password")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "The {0} must be at least {2} characters long.")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
