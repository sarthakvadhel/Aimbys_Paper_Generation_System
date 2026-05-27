namespace Aimbys.Domain.Enums;

public enum ExamEventType
{
    Started = 0,
    FullscreenEntered = 1,
    FullscreenExit = 2,
    TabBlur = 3,
    TabFocus = 4,
    BrowserResize = 5,
    PasteAttempt = 6,
    KeyboardShortcut = 7,
    ConnectionLost = 8,
    ConnectionRestored = 9,
    Heartbeat = 10,
    AutoSubmitted = 11,
    ManualSubmitted = 12,
    SuspiciousActivity = 13
}
