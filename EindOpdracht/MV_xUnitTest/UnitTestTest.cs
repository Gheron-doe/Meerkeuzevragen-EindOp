using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;
using MV_BL.Services;
using MV_xUnitTest.Fakes;

namespace MV_xUnitTest;


public class UnitTestTest
{
	// Setup

	private static (TestService svc, FakeQuestionRepository qRepo, FakeTestRepository tRepo, int topicId) Setup()
	{
		var qRepo   = new FakeQuestionRepository();
		var tRepo   = new FakeTestRepository();
		const int topicId = 1;

		for (int i = 0; i < 5; i++)
		{
			qRepo.Add(new Question
			{
				TopicId        = topicId,
				QuestionText   = $"Q{i}?",
				DifficultyLevel = 1,
				IsActive       = true,
				Answers = new List<Answer>
				{
					new() { AnswerText = "a", IsCorrect = true,  OriginalOrder = 0 },
					new() { AnswerText = "b", IsCorrect = false, OriginalOrder = 1 },
					new() { AnswerText = "c", IsCorrect = false, OriginalOrder = 2 },
					new() { AnswerText = "d", IsCorrect = false, OriginalOrder = 3 }
				}
			});
		}
		return (new TestService(tRepo, qRepo, new Random(42)), qRepo, tRepo, topicId);
	}

	// GenerateTest: questionCount

	[Theory]
	[InlineData(1)]
	[InlineData(3)]
	[InlineData(5)]
	public void Test_GenerateTest_QuestionCount_Valid(int count)
	{
		var (svc, _, _, topicId) = Setup();
		var test = svc.GenerateTest(topicId, count, "T");

		Assert.Equal(count, test.Questions.Count);
	}

	[Fact]
	public void Test_GenerateTest_NoQuestions_Invalid()
	{
		var svc = new TestService(new FakeTestRepository(), new FakeQuestionRepository());
		Assert.Throws<NoQuestionsAvailableException>(() =>
			svc.GenerateTest(topicId: 1, questionCount: 5, title: "x"));
	}

	// GenerateTest: answer display order shuffled

	[Fact]
	public void Test_GenerateTest_AnswerDisplayOrder_Valid()
	{
		var (svc, _, _, topicId) = Setup();
		var test = svc.GenerateTest(topicId, 3, "Shuffled");

		Assert.All(test.Questions, tq => Assert.Equal(4, tq.AnswerDisplayOrder.Count));
	}

	// GenerateTest: scoring strategy

	[Theory]
	[InlineData(ScoringMode.SimplePercent)]
	public void Test_GenerateTest_ScoringMode_Valid(ScoringMode mode)
	{
		var (svc, _, tRepo, topicId) = Setup();
		var test = svc.GenerateTest(topicId, 2, "Scored", scoringStrategy: mode);

		Assert.Equal(mode, tRepo.GetById(test.Id)!.ScoringStrategy);
	}

	// Deactivate

	[Fact]
	public void Test_Deactivate_SetsIsFlagged_Valid()
	{
		var (svc, _, tRepo, topicId) = Setup();
		var test = svc.GenerateTest(topicId, 2, "Flag me");
		svc.Deactivate(test.Id);

		Assert.True(tRepo.GetById(test.Id)!.IsFlagged);
	}
}
