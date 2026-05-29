namespace MV_WPF;

public class GradingRow
{
    public int      AttemptId      { get; init; }
    public int      UserId         { get; init; }
    public string   Username       { get; init; } = string.Empty;
    public int      TestId         { get; init; }
    public string   TestTitle      { get; init; } = string.Empty;
    public string   TopicNames     { get; init; } = string.Empty;
    public string   Difficulties   { get; init; } = string.Empty;
    public int      CorrectCount   { get; init; }
    public int      TotalQuestions { get; init; }
    public double   DisplayScore   { get; init; }

    public string ScoreText => $"{DisplayScore:F1}%";

    public DateTime  StartedAt    { get; init; }
    public DateTime? CompletedAt  { get; init; }

    public bool IsSelected { get; set; }
    public string? Feedback { get; set; }
}
