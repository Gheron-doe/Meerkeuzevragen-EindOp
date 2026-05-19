using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_BL.Interfaces;

public interface IAttemptRepository
{
	IReadOnlyList<TestAttempt> GetAll();
    IReadOnlyList<TestAttempt> GetByUser(int userId);
	IReadOnlyList<TestAttempt> GetByTest(int testId);
	TestAttempt? GetById(int id);
	int StartAttempt(TestAttempt attempt);
    void Complete(int attemptId, DateTime completedAt, int correctCount, IEnumerable<AttemptAnswer> answers);
    void UpdateFeedback(int attemptId, string? feedback);
}
