namespace MV_BL.Domain;

public class TestAttempt
{
    public int Id { get; set; }
    public int TestId { get; set; }
    public int UserId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int? Score { get; set; } // count of correct answers
    public string? Feedback { get; set; }
    public List<AttemptAnswer> Answers { get; set; } = new();
}
