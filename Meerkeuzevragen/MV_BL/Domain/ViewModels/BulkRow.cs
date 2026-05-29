namespace MV_BL.Domain.ViewModels;

public class BulkRow
{
    public BulkRow(int userId, string answers, string? feedback = null)
    {
        UserId = userId;
        Answers = answers;
        Feedback = feedback;
    }
    public int UserId { get; }
    public string Answers { get; }
    public string? Feedback { get; }
}
