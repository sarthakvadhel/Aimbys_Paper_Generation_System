using Aimbys.Application.Coding;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Coding;

/// <summary>
/// V1 logging stub for <see cref="IPlagiarismScorer"/>.
/// Real scoring (MOSS, AST-diff, or ML-based approaches) will be
/// wired in a future chunk.
/// </summary>
public sealed class PlagiarismScorer : IPlagiarismScorer
{
    private readonly ILogger<PlagiarismScorer> _logger;

    public PlagiarismScorer(ILogger<PlagiarismScorer> logger)
    {
        _logger = logger;
    }

    public Task<double> ScoreAsync(string sourceCode, IReadOnlyList<string> existingSubmissions, CancellationToken ct = default)
    {
        _logger.LogInformation("PlagiarismScorer: scoring not implemented in V1. Returning 0.0 (no similarity).");
        return Task.FromResult(0.0);
    }
}
