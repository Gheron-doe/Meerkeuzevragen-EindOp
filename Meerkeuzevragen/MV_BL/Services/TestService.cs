using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_BL.Services;

public class TestService
{
    private readonly ITestRepository _repo;

    public TestService(ITestRepository repo) => _repo = repo;

    public List<Test> GetAll() => _repo.GetAll();

    public Test? GetById(int id) => _repo.GetById(id);

    public List<int> GetTestDifficulties(int testId) => _repo.GetTestDifficulties(testId);

    public void Add(Test test) => _repo.Add(test);

    public void Flag(int id) => _repo.Flag(id);
}
