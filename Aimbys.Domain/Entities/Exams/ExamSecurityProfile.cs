namespace Aimbys.Domain.Entities.Exams;

public class ExamSecurityProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ExamId { get; set; }
    public bool RequireFullscreen { get; set; } = true;
    public bool DetectTabSwitch { get; set; } = true;
    public bool DetectResize { get; set; }
    public bool BlockCopyPaste { get; set; } = true;
    public bool BlockKeyboardShortcuts { get; set; } = true;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public int MaxConnectionLossSeconds { get; set; } = 120;
    public bool AutoSubmitOnTimeout { get; set; } = true;
    public bool TrackDevice { get; set; } = true;
    public bool TrackSession { get; set; } = true;
    public Exam? Exam { get; set; }
}
