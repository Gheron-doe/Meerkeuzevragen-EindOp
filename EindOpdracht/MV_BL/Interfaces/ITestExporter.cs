using MV_BL.Domain;

namespace MV_BL.Interfaces;

public interface ITestExporter
{
	string Format { get; }
    void Export(Test test, string filePath, bool includeAnswers = false);
}