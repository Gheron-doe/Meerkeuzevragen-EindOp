namespace MV_WPF;

public class RunnerQuestion
{
    public string Header { get; init; } = string.Empty;
    public string QuestionText { get; init; } = string.Empty;
    public int TestQuestionId { get; init; }
    public List<AnswerOption> AnswerOptions { get; init; } = new();
}
