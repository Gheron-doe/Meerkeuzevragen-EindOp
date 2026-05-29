using MV_BL.Exceptions;
using MV_BL.Interfaces;
using MV_Util.Implementations;

namespace MV_Util.Factories;

public static class ExporterFactory
{
    public static IEnumerable<string> AvailableFormats => new[] { "txt" };

    public static ITestExporter Create(string format)
        => (format ?? string.Empty).ToLowerInvariant() switch
        {
            "txt" => new TxtTestExporter(),
            _ => throw new ImportException($"No exporter registered for format '{format}'.")
        };
}
