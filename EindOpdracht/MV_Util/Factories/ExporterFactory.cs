using MV_BL.Exceptions;
using MV_BL.Interfaces;
using MV_Util.Registries;

namespace MV_Util.Factories
{
    public class ExporterFactory
    {
        private readonly Dictionary<string, ITestExporter> _exporters = new(StringComparer.OrdinalIgnoreCase);

        public ExporterFactory()
        {
            Register(new TxtTestExporter());
        }

        public void Register(ITestExporter exporter) => _exporters[exporter.Format] = exporter;

        public IEnumerable<string> AvailableFormats => _exporters.Keys;

        public ITestExporter Create(string format)
        {
            if (_exporters.TryGetValue(format, out var exporter)) return exporter;
            throw new UnsupportedFormatException(format, nameof(ExporterFactory));
        }
    }
}
