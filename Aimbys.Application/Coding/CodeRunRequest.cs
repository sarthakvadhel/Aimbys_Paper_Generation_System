namespace Aimbys.Application.Coding;

public sealed record CodeRunRequest(
    string Language,
    string SourceCode,
    Guid? QuestionId,
    bool SampleOnly = true);
