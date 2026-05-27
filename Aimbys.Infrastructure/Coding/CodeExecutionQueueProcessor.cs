using Aimbys.Application.Scheduling;
using Microsoft.Extensions.Logging;

namespace Aimbys.Infrastructure.Coding;

/// <summary>
/// Scheduled job handler that processes the <c>CodeExecutionQueues</c>
/// table. V1: logs and returns — real execution wiring arrives in a
/// future chunk.
/// </summary>
public sealed class CodeExecutionQueueProcessor : IScheduledJobHandler
{
    public const string Key = "coding.execution.process";
    public const string DefaultCron = "*/2 * * * *"; // every 2 minutes

    public string JobKey => Key;

    private readonly ILogger<CodeExecutionQueueProcessor> _logger;

    public CodeExecutionQueueProcessor(ILogger<CodeExecutionQueueProcessor> logger)
    {
        _logger = logger;
    }

    public Task ExecuteAsync(string? payload, CancellationToken cancellationToken)
    {
        _logger.LogInformation("No pending code execution queue items");
        return Task.CompletedTask;
    }
}
