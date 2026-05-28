using System.ComponentModel.DataAnnotations;

namespace Aimbys.Web.ViewModels.Account;

/// <summary>
/// View model for the first-login mandatory password-change screen.
/// The <c>InstituteLoginId</c> field acts as a "current password"
/// confirmation: the admin must prove they received the 8-digit code
/// shared by the Super Admin before they can set a new password.
/// </summary>
public class SetFirstPasswordViewModel
{
    [Required(ErrorMessage = "Please enter your Institute Login ID.")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "The Institute Login ID is exactly 8 digits.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "The Institute Login ID must be exactly 8 numeric digits.")]
    [Display(Name = "Institute Login ID (8-digit initial password)")]
    public string InstituteLoginId { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your new password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm New Password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
