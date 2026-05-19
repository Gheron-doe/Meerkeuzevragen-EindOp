namespace MV_BL.Interfaces;

public record BulkRow(int UserId, string Answers, string? Feedback = null);

public interface IBulkInputParser
{
    string Format { get; }
    IEnumerable<BulkRow> Parse(string filePath);
}