using MV_BL.Interfaces;

namespace MV_BL.Domain;

public class Test
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? Difficulty { get; set; }
    public ScoringMode ScoringStrategy { get; set; } = ScoringMode.SimplePercent;
    public bool IsFlagged { get; set; } = false; // true = soft-deleted
    public List<TestQuestion> Questions { get; set; } = new();
}
