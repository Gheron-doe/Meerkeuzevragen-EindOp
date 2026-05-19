using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;
using MV_BL.Services;
using MV_xUnitTest.Fakes;

namespace MV_xUnitTest;

public class UnitTestImport
{
	// Setup 

	private static (ImportService svc, FakeQuestionRepository qRepo, int topicId) Setup()
	{
		var qRepo   = new FakeQuestionRepository();
		var tRepo   = new FakeTopicRepository();
		var topicId = tRepo.Add(new Topic { Name = "Import Topic" });
		return (new ImportService(qRepo, tRepo), qRepo, topicId);
	}

	private static FakeImporter MakeImporter(int questionCount = 2) =>
		new(Enumerable.Range(1, questionCount).Select(i => new Question
		{
			QuestionText    = $"Q{i}?",
			DifficultyLevel = 1,
			IsFlagged       = false,
			IsActive        = true,
			Answers = new List<Answer>
			{
				new() { AnswerText = "Correct", IsCorrect = true,  OriginalOrder = 0 },
				new() { AnswerText = "Wrong",   IsCorrect = false, OriginalOrder = 1 }
			}
		}));

	// ParseAndSeed: difficulty 

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void Test_ParseAndSeed_Difficulty_Valid(int difficulty)
	{
		var (svc, qRepo, topicId) = Setup();
		svc.ParseAndSeed("fake.txt", topicId, MakeImporter(3), difficulty);

		Assert.All(qRepo.GetByTopic(topicId), q => Assert.Equal(difficulty, q.DifficultyLevel));
	}

	[Theory]
	[InlineData(0)]
	[InlineData(4)]
	[InlineData(-1)]
	public void Test_ParseAndSeed_Difficulty_Invalid(int difficulty)
	{
		var (svc, _, topicId) = Setup();
		Assert.Throws<InvalidDifficultyException>(() =>
			svc.ParseAndSeed("fake.txt", topicId, MakeImporter(), difficulty));
	}

	// ParseAndSeed: default difficulty

	[Fact]
	public void Test_ParseAndSeed_DefaultDifficulty_Valid()
	{
		var (svc, qRepo, topicId) = Setup();
		svc.ParseAndSeed("fake.txt", topicId, MakeImporter());

		Assert.All(qRepo.GetByTopic(topicId), q => Assert.Equal(1, q.DifficultyLevel));
	}

	// ParseAndSeed: topicId

	[Theory]
	[InlineData(999)]
	[InlineData(-1)]
	[InlineData(0)]
	public void Test_ParseAndSeed_TopicId_Invalid(int badTopicId)
	{
		var (svc, _, _) = Setup();
		Assert.Throws<TopicNotFoundException>(() =>
			svc.ParseAndSeed("fake.txt", badTopicId, MakeImporter()));
	}

	// ParseAndSeed: initial state

	[Fact]
	public void Test_ParseAndSeed_InitialState_Valid()
	{
		var (svc, qRepo, topicId) = Setup();
		svc.ParseAndSeed("fake.txt", topicId, MakeImporter(1));

		var q = qRepo.GetByTopic(topicId).Single();
		Assert.True(q.IsActive);
		Assert.False(q.IsFlagged);
	}

	// ParseAndSeed: return count

	[Theory]
	[InlineData(1)]
	[InlineData(3)]
	[InlineData(5)]
	public void Test_ParseAndSeed_ReturnCount_Valid(int questionCount)
	{
		var (svc, _, topicId) = Setup();
		int count = svc.ParseAndSeed("fake.txt", topicId, MakeImporter(questionCount));

		Assert.Equal(questionCount, count);
	}
}

// FakeImporter

public class FakeImporter : IQuestionImporter
{
	private readonly List<Question> _questions;
	public string Format => "txt";
	public FakeImporter(IEnumerable<Question> questions) => _questions = questions.ToList();
	public bool CanImport(string filePath) => true;
	public IReadOnlyList<Question> Import(string filePath, int topicId)
	{
		foreach (var q in _questions) q.TopicId = topicId;
		return _questions;
	}
}
