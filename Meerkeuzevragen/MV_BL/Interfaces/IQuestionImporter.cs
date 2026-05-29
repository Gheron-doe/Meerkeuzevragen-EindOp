using MV_BL.Domain;

namespace MV_BL.Interfaces;

public interface IQuestionImporter
{
    string Format { get; }
    IReadOnlyList<Question> Import(string filePath);
}
