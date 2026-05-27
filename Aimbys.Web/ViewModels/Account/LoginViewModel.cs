using System.ComponentModel.DataAnnotations;

namespace Aimbys.Web.ViewModels.Account;

public class LoginViewModel
{
    [Required, EmailAddress, Display(Name = "Email")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
