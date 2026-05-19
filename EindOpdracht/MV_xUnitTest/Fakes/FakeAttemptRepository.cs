using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_xUnitTest.Fakes;

public class FakeAttemptRepository : IAttemptRepository
{
	public List<TestAttempt> Store { get; } = new();
	private int _next = 1;

	public IReadOnlyList<TestAttempt> GetAll() => Store.ToList();
	public IReadOnlyList<TestAttempt> GetByUser(int userId) => Store.Where(a => a.UserId == userId).ToList();
	public IReadOnlyList<TestAttempt> GetByTest(int testId) => Store.Where(a => a.TestId == testId).ToList();
	public TestAttempt? GetById(int id) => Store.FirstOrDefault(a => a.Id == id);

	public int StartAttempt(TestAttempt attempt)
	{
		attempt.Id = _next++;
		Store.Add(attempt);
		return attempt.Id;
	}

	public void Complete(int attemptId, DateTime completedAt, int correctCount, IEnumerable<AttemptAnswer> answers)
	{
		var a = Store.FirstOrDefault(x => x.Id == attemptId);
		if (a is null) return;
		a.CompletedAt = completedAt;
		a.Score = correctCount;
		a.Answers = answers.ToList();
	}

	public void UpdateFeedback(int attemptId, string? feedback)
	{
		var a = Store.FirstOrDefault(x => x.Id == attemptId);
		if (a is not null) a.Feedback = feedback;
	}
}
