using MV_BL.Exceptions;
using MV_BL.Interfaces;
using MV_Util.Registries;

namespace MV_Util.Factories
{
    public class BulkInputParserFactory
    {
        private readonly Dictionary<string, IBulkInputParser> _parsers = new(StringComparer.OrdinalIgnoreCase);

        public BulkInputParserFactory()
        {
            Register(new CsvBulkInputParser());
        }

        public void Register(IBulkInputParser parser) => _parsers[parser.Format] = parser;

        public IEnumerable<string> AvailableFormats => _parsers.Keys;

        public IBulkInputParser Create(string format)
        {
            if (_parsers.TryGetValue(format, out var parser)) return parser;
            throw new UnsupportedFormatException(format, nameof(BulkInputParserFactory));
        }
    }
}
