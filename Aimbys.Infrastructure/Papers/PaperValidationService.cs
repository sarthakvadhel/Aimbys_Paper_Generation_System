using Aimbys.Application.Papers;

namespace Aimbys.Infrastructure.Papers;

/// <summary>
/// Validates a paper save request against business rules:
/// - No duplicate QuestionIds
/// - Total marks from questions match declared total
/// - At least one question per section
/// </summary>
public class PaperValidationService : IPaperValidationService
{
    public PaperValidationResult Validate(PaperSaveRequest request, int declaredTotalMarks)
    {
        var errors = new List<string>();

        // 1. No duplicate QuestionIds
        var questionIds = request.Questions.Select(q => q.QuestionId).ToList();
        var distinctCount = questionIds.Distinct().Count();
        if (distinctCount < questionIds.Count)
        {
            errors.Add("Duplicate questions detected. Each question can only appear once in a paper.");
        }

        // 2. Total marks from questions match declared total
        // Only validate when at least one question has an explicit override;
        // if all are null the caller relies on the question's own marks
        // (resolved at render time) so we skip the check.
        var hasAnyOverride = request.Questions.Any(q => q.MarksOverride.HasValue);
        if (hasAnyOverride)
        {
            var actualTotal = request.Questions.Sum(q => q.MarksOverride ?? 0m);
            if (actualTotal != declaredTotalMarks)
            {
                errors.Add($"Total marks mismatch: questions sum to {actualTotal} but paper declares {declaredTotalMarks}.");
            }
        }

        // 3. At least one question per section (only when sections are declared)
        if (request.Sections.Count > 0 && request.Questions.Count > 0)
        {
            var sectionIndicesWithQuestions = request.Questions
                .Select(q => q.SectionIndex)
                .Distinct()
                .ToHashSet();

            for (int i = 0; i < request.Sections.Count; i++)
            {
                if (!sectionIndicesWithQuestions.Contains(i))
                    errors.Add($"Section '{request.Sections[i].Name}' has no questions assigned.");
            }
        }

        return new PaperValidationResult(errors.Count == 0, errors);
    }
}
