using MV_BL.Exceptions;
using MV_BL.Interfaces;
using MV_Util.Implementations;

namespace MV_Util.Factories;

public static class ImporterFactory
{
    public static IEnumerable<string> AvailableFormats => new[] { "txt" };

    public static IQuestionImporter Create(string format)
        => (format ?? string.Empty).ToLowerInvariant() switch
        {
            "txt" => new TxtQuestionImporter(),
            _ => throw new ImportException($"No importer registered for format '{format}'.")
        };
}
