using MV_BL.Domain;

namespace MV_BL.Interfaces;

public interface ITestRepository
{
    List<Test> GetAll();

    Test? GetById(int id);

    List<int> GetTestDifficulties(int testId);

    void Add(Test test);

    void Flag(int id);
}
