using MV_BL.Domain;
using MV_BL.Interfaces;

namespace MV_xUnitTest.Fakes;

public class FakeQuestionRepository : IQuestionRepository
{
	private readonly List<Question> _store = new();
	private int _next    = 1;
	private int _ansNext = 1;

	public IReadOnlyList<Question> GetAll(bool excludeFlagged = true)
	{
		var q = _store.AsEnumerable();
		if (excludeFlagged) q = q.Where(x => !x.IsFlagged);
		return q.ToList();
	}

	public IReadOnlyList<Question> GetByTopic(int topicId, bool excludeFlagged = true, bool activeOnly = false, int? difficulty = null)
	{
		var q = _store.Where(x => x.TopicId == topicId);
		if (excludeFlagged)      q = q.Where(x => !x.IsFlagged);
		if (activeOnly)          q = q.Where(x => x.IsActive);
		if (difficulty.HasValue) q = q.Where(x => x.DifficultyLevel == difficulty.Value);
		return q.ToList();
	}

	public Question? GetById(int id) => _store.FirstOrDefault(q => q.Id == id);

	public int Add(Question question)
	{
		question.Id = _next++;
		foreach (var a in question.Answers.Where(a => a.Id == 0))
			a.Id = _ansNext++;
		_store.Add(question);
		return question.Id;
	}

	public void UpdateWithAnswers(Question question)
	{
		var existing = GetById(question.Id);
		if (existing is null) return;
		existing.QuestionText    = question.QuestionText;
		existing.DifficultyLevel = question.DifficultyLevel;
		existing.Feedback        = question.Feedback;

		var rebuilt = new List<Answer>();
		foreach (var incoming in question.Answers.ToList())
		{
			rebuilt.Add(new Answer
			{
				Id            = incoming.Id > 0 ? incoming.Id : _ansNext++,
				QuestionId    = existing.Id,
				AnswerText    = incoming.AnswerText,
				IsCorrect     = incoming.IsCorrect,
				OriginalOrder = incoming.OriginalOrder,
				Feedback      = incoming.Feedback
			});
		}
		existing.Answers = rebuilt;
	}

	public void Deactivate(int id)
	{
		var q = GetById(id);
		if (q is not null) q.IsActive = false;
	}

	public void Activate(int id)
	{
		var q = GetById(id);
		if (q is not null) q.IsActive = true;
	}

	public void Flag(int id)
	{
		var q = GetById(id);
		if (q is not null) q.IsFlagged = true;
	}
}
