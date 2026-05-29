using MV_BL.Exceptions;
using MV_BL.Interfaces;
using MV_Util.Implementations;

namespace MV_Util.Factories;

public static class BulkInputParserFactory
{
    public static IEnumerable<string> AvailableFormats => new[] { "csv" };

    public static IBulkInputParser Create(string format)
        => (format ?? string.Empty).ToLowerInvariant() switch
        {
            "csv" => new CsvBulkInputParser(),
            _ => throw new ImportException($"No bulk parser registered for format '{format}'.")
        };
}
