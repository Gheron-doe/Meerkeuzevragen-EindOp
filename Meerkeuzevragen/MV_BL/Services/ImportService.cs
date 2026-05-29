using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_BL.Services;

public class ImportService
{
    private readonly IQuestionRepository _qRepo;

    public ImportService(IQuestionRepository qRepo) => _qRepo = qRepo;
    public IReadOnlyList<Question> Parse(string filePath, IQuestionImporter importer)
        => importer.Import(filePath);
    public void Persist(IEnumerable<Question> questions)
    {
        foreach (var q in questions) _qRepo.Add(q);
    }
}
