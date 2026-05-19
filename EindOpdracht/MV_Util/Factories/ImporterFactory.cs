using MV_BL.Exceptions;
using MV_BL.Interfaces;
using MV_Util.Registries;

namespace MV_Util.Factories
{
    public class ImporterFactory
    {
        private readonly List<IQuestionImporter> _importers = new();

        public ImporterFactory()
        {
            Register(new TxtQuestionImporter());
        }

        public void Register(IQuestionImporter importer)
        {
            var existing = _importers.FindIndex(i => string.Equals(i.Format, importer.Format, StringComparison.OrdinalIgnoreCase));
            if (existing >= 0)
                _importers[existing] = importer; // replace
            else
                _importers.Add(importer);
        }

        public IEnumerable<string> AvailableFormats => _importers.Select(i => i.Format);

        public IQuestionImporter Create(string format)
        {
            var importer = _importers.FirstOrDefault(i => string.Equals(i.Format, format, StringComparison.OrdinalIgnoreCase));
            if (importer is null)
                throw new UnsupportedFormatException(format, nameof(ImporterFactory));
            return importer;
        }

        public IQuestionImporter? CreateForFile(string filePath)
            => _importers.FirstOrDefault(i => i.CanImport(filePath));
    }
}
