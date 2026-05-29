namespace MV_WPF;

public class BulkImportResultRow
{
    public int UserId { get; init; }
    public int CorrectCount { get; init; }
    public int Total { get; init; }
    public bool Persisted { get; init; }
    public string Wrong { get; init; } = string.Empty;
}
