namespace MV_WPF;

public class ViewerQuestion
{
    public string Header { get; init; } = string.Empty;
    public string QuestionText { get; init; } = string.Empty;
    public List<ViewerAnswer> Answers { get; init; } = new();
}
