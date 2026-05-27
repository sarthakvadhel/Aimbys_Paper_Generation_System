namespace Aimbys.Application.Papers;

public interface IPaperValidationService
{
    PaperValidationResult Validate(PaperSaveRequest request, int declaredTotalMarks);
}
