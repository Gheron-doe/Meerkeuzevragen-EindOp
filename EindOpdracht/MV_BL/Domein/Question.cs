namespace MV_BL.Domain;

public class Question
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public int DifficultyLevel { get; set; } = 1;
    public bool IsFlagged { get; set; } = false; // true = soft-deleted
    public bool IsActive { get; set; } = true;   // false = deactivated
    public DateTime? CreatedAt { get; set; }
    public string? Feedback { get; set; }
    public List<Answer> Answers { get; set; } = new();
}
