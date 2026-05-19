namespace MV_BL.Domain;

public class Topic
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsFlagged { get; set; } = false; // true = soft-deleted
}
