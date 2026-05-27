using Aimbys.Application.Blueprints;

namespace Aimbys.Infrastructure.Blueprints;

public class BlueprintValidator : IBlueprintValidator
{
    public BlueprintValidationResult Validate(BlueprintEditRequest request, int totalMarks)
    {
        var errors = new List<string>();

        if (request.Sections.Count == 0)
        {
            errors.Add("At least one section is required.");
        }

        foreach (var section in request.Sections)
        {
            if (string.IsNullOrWhiteSpace(section.Name))
                errors.Add($"Section at sort order {section.SortOrder} has an empty name.");

            if (section.Marks <= 0)
                errors.Add($"Section '{section.Name}' must have positive marks.");
        }

        var sectionMarksSum = request.Sections.Sum(s => s.Marks);
        if (sectionMarksSum != totalMarks)
        {
            errors.Add($"Section marks total ({sectionMarksSum}) does not match blueprint total marks ({totalMarks}).");
        }

        return new BlueprintValidationResult(errors.Count == 0, errors);
    }
}
