using MV_BL.Domain;

namespace MV_BL.Interfaces;

public interface IQuestionImporter
{
	string Format { get; }
	bool CanImport(string filePath);
	IReadOnlyList<Question> Import(string filePath, int topicId);
}
