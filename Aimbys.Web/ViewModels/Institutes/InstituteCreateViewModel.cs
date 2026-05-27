using System.ComponentModel.DataAnnotations;
using Aimbys.Domain.Enums;

namespace Aimbys.Web.ViewModels.Institutes;

public class InstituteCreateViewModel
{
    [Required, StringLength(200), Display(Name = "Institute Name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50), Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Required, Display(Name = "Type")]
    public InstituteType Type { get; set; } = InstituteType.School;

    [Required, StringLength(120), Display(Name = "City")]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(120), Display(Name = "State")]
    public string State { get; set; } = string.Empty;

    [Required, StringLength(2), Display(Name = "Country")]
    public string Country { get; set; } = "IN";

    [Required, EmailAddress, StringLength(256), Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;

    [Phone, StringLength(32), Display(Name = "Contact Phone")]
    public string? ContactPhone { get; set; }

    [Required, Display(Name = "License Tier")]
    public LicenseTier LicenseTier { get; set; } = LicenseTier.Standard;

    [Required, EmailAddress, StringLength(256), Display(Name = "Admin Email")]
    public string AdminEmail { get; set; } = string.Empty;
}
