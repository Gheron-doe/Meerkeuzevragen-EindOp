namespace MV_WPF;

public class ResultQuestionRow
{
    public int Order { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public bool IsSkipped { get; init; }
    public string? QuestionFeedbackText { get; init; }
    public string? SelectedAnswerFeedback { get; init; }
    public string? CorrectAnswerFeedback { get; init; }
    public List<ResultAnswerRow> Options { get; } = new();
}
