namespace Aimbys.Application.Coding;

public interface IPlagiarismScorer
{
    Task<double> ScoreAsync(string sourceCode, IReadOnlyList<string> existingSubmissions, CancellationToken ct = default);
}
