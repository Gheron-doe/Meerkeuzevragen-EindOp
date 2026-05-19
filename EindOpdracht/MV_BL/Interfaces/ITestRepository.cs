using MV_BL.Domain;

namespace MV_BL.Interfaces;

public interface ITestRepository
{
	IReadOnlyList<Test> GetAll();
	Test? GetById(int id);
	int Add(Test test);
	void Deactivate(int id);
}
