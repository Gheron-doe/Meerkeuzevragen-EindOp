namespace MV_BL.Helpers;

public static class AnswerOrderSerializer
{
    public static string Serialize(IEnumerable<int> order)
        => string.Join(",", order);

    public static List<int> Deserialize(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return new List<int>();
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.Parse(s.Trim()))
            .ToList();
    }
}
