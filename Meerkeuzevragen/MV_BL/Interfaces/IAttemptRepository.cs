using MV_BL.Domain;

namespace MV_BL.Interfaces;

public interface IAttemptRepository
{
    List<TestAttempt> GetAll();
    List<TestAttempt> GetByUser(int userId);
    List<TestAttempt> GetByTest(int testId);
    TestAttempt? GetById(int attemptId);
    void Save(TestAttempt attempt, List<AttemptAnswer> answers);
    void UpdateFeedback(int attemptId, string? feedback);
    void BulkInsertAttempts(IEnumerable<(TestAttempt attempt, List<AttemptAnswer> answers)> batch);
}
