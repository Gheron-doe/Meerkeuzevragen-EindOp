namespace MV_WPF;

public class ResultAnswerRow
{
    public string Label { get; init; } = string.Empty;
    public string AnswerText { get; init; } = string.Empty;
    public bool IsUserPick { get; init; }
    public bool IsCorrect { get; init; }
    public string? Feedback { get; init; }
}
