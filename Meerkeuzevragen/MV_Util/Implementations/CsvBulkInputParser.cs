using MV_BL.Domain.ViewModels;
using MV_BL.Interfaces;

namespace MV_Util.Implementations;

public class CsvBulkInputParser : IBulkInputParser
{
    public string Format => "csv";

    public IEnumerable<BulkRow> Parse(string filePath)
    {
        bool first = true;
        foreach (var line in File.ReadLines(filePath))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            if (first)
            {
                first = false;
                if (trimmed.StartsWith("ID", StringComparison.OrdinalIgnoreCase)) continue;
            }

            var parts = trimmed.Split(',', 3);
            if (parts.Length < 2) continue;
            if (!int.TryParse(parts[0].Trim(), out int userId)) continue;

            string answers = parts[1].Trim().ToUpperInvariant();
            string? feedback = parts.Length >= 3 ? parts[2].Trim() : null;
            yield return new BulkRow(userId, answers, string.IsNullOrEmpty(feedback) ? null : feedback);
        }
    }
}
