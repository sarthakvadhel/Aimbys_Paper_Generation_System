namespace Aimbys.Application.Blueprints;

public interface IBlueprintValidator
{
    BlueprintValidationResult Validate(BlueprintEditRequest request, int totalMarks);
}
