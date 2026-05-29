namespace MV_WPF;

public class AnswerOption
{
    public string Label { get; init; } = string.Empty;
    public int AnswerId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public bool IsSelected { get; set; }
}
