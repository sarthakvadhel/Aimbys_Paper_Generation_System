using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Aimbys.Web.ViewModels.Users;

public class UserInviteViewModel
{
    [Required]
    [StringLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    public Guid? DepartmentId { get; set; }

    public Guid? ClassBatchId { get; set; }

    // Dropdowns
    public SelectList? Roles { get; set; }
    public SelectList? Departments { get; set; }
    public SelectList? ClassBatches { get; set; }
}
