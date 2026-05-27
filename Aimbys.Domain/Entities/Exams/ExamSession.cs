namespace Aimbys.Domain.Entities.Exams;

public class ExamSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AttemptId { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastHeartbeatAtUtc { get; set; } = DateTime.UtcNow;
    public ExamAttempt? Attempt { get; set; }
}
