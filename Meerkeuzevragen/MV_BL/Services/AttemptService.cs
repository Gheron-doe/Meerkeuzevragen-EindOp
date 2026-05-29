using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_BL.Services;

public class AttemptService
{
    private readonly IAttemptRepository _repo;

    public AttemptService(IAttemptRepository repo) => _repo = repo;

    public List<TestAttempt> GetAll() => _repo.GetAll();

    public List<TestAttempt> GetByUser(int userId) => _repo.GetByUser(userId);

    public List<TestAttempt> GetByTest(int testId) => _repo.GetByTest(testId);

    public TestAttempt? GetById(int id) => _repo.GetById(id);

    public void Save(TestAttempt attempt, List<AttemptAnswer> answers)
        => _repo.Save(attempt, answers);

    public void UpdateFeedback(int attemptId, string? feedback)
        => _repo.UpdateFeedback(attemptId, feedback);

    public void BulkInsertAttempts(IEnumerable<(TestAttempt attempt, List<AttemptAnswer> answers)> batch)
        => _repo.BulkInsertAttempts(batch);
}
