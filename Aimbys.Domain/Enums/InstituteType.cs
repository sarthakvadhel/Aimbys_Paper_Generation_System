using System.ComponentModel.DataAnnotations;

namespace Aimbys.Domain.Enums;

/// <summary>
/// Institute taxonomy. Names line up with the values used in the React
/// reference (`src/app/components/aimbys/superadmin/InstituteManagement.tsx`):
/// "Coaching", "School Chain", "Government", "State Board", "University".
/// </summary>
public enum InstituteType
{
    [Display(Name = "Coaching")]
    Coaching = 0,

    [Display(Name = "School")]
    School = 1,

    [Display(Name = "School Chain")]
    SchoolChain = 2,

    [Display(Name = "College")]
    College = 3,

    [Display(Name = "University")]
    University = 4,

    [Display(Name = "State Board")]
    StateBoard = 5,

    [Display(Name = "Government")]
    Government = 6,

    [Display(Name = "Other")]
    Other = 7
}
