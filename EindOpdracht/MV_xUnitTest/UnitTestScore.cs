using MV_BL.Domain;
using MV_BL.Services;
using MV_Util.Registries;

namespace MV_xUnitTest;

public class UnitTestScore
{
	// Setup

	private static (Test test, int correctAnswerId, int wrongAnswerId) BuildTest()
	{
		var q = new Question
		{
			Id = 10, QuestionText = "Q?",
			Answers = new List<Answer>
			{
				new() { Id = 1, AnswerText = "Ans-A", IsCorrect = true,  OriginalOrder = 0 },
				new() { Id = 2, AnswerText = "Ans-B", IsCorrect = false, OriginalOrder = 1 },
				new() { Id = 3, AnswerText = "Ans-C", IsCorrect = false, OriginalOrder = 2 },
				new() { Id = 4, AnswerText = "Ans-D", IsCorrect = false, OriginalOrder = 3 }
			}
		};
		var test = new Test
		{
			Id = 1, Title = "T",
			Questions = new List<TestQuestion>
			{
				new()
				{
					Id = 100, TestId = 1, QuestionId = 10, QuestionOrder = 1,
					AnswerDisplayOrder = new List<int> { 3, 1, 0, 2 }, // C=orig0=Id1(correct)
					Question = q
				}
			}
		};
		return (test, correctAnswerId: 1, wrongAnswerId: 4);
	}

	private static (Test test, int correctAnswerId, int wrongAnswerId) BuildTestWithFeedback()
	{
		var q = new Question
		{
			Id = 20, QuestionText = "Q with feedback?",
			Feedback = "Question-level hint",
			Answers = new List<Answer>
			{
				new() { Id = 10, AnswerText = "Right", IsCorrect = true,  OriginalOrder = 0, Feedback = "Correct feedback" },
				new() { Id = 11, AnswerText = "Wrong", IsCorrect = false, OriginalOrder = 1, Feedback = "Wrong choice feedback" }
			}
		};
		var test = new Test
		{
			Id = 2, Title = "TF",
			Questions = new List<TestQuestion>
			{
				new()
				{
					Id = 200, TestId = 2, QuestionId = 20, QuestionOrder = 1,
					AnswerDisplayOrder = new List<int> { 0, 1 }, // A=correct, B=wrong
					Question = q
				}
			}
		};
		return (test, correctAnswerId: 10, wrongAnswerId: 11);
	}

	// Grade: answer result

	[Fact]
	public void Test_Grade_Correct_Valid()
	{
		var (test, correctId, _) = BuildTest();
		var tqId   = test.Questions[0].Id;
		var result = ScoreService.Grade(test, new[] { (tqId, (int?)correctId) });

		Assert.Equal(1, result.CorrectCount);
		Assert.True(result.Answers[0].IsCorrect);
	}

	[Fact]
	public void Test_Grade_Wrong_Valid()
	{
		var (test, _, wrongId) = BuildTest();
		var tqId   = test.Questions[0].Id;
		var result = ScoreService.Grade(test, new[] { (tqId, (int?)wrongId) });

		Assert.Equal(0, result.CorrectCount);
		Assert.False(result.Answers[0].IsCorrect);
	}

	[Fact]
	public void Test_Grade_Skipped_Valid()
	{
		var (test, _, _) = BuildTest();
		var tqId   = test.Questions[0].Id;
		var result = ScoreService.Grade(test, new[] { (tqId, (int?)null) });

		Assert.Equal(0, result.CorrectCount);
		Assert.False(result.Answers[0].IsCorrect);
	}

	// Grade: correct-letter feedback field

	[Fact]
	public void Test_Grade_CorrectLetter_Valid()
	{
		var (test, _, wrongId) = BuildTest();
		var tqId   = test.Questions[0].Id;
		var result = ScoreService.Grade(test, new[] { (tqId, (int?)wrongId) });

		Assert.Equal("C", result.Feedback[0].CorrectLetter); // slot C is correct in BuildTest
	}

	// Grade: feedback fields

	[Fact]
	public void Test_Grade_WrongAnswer_FeedbackFields_Valid()
	{
		var (test, _, wrongId) = BuildTestWithFeedback();
		var tqId   = test.Questions[0].Id;
		var result = ScoreService.Grade(test, new[] { (tqId, (int?)wrongId) });

		var fb = result.Feedback[0];
		Assert.Equal("Question-level hint",   fb.QuestionFeedbackText);
		Assert.Equal("Wrong choice feedback", fb.SelectedAnswerFeedback);
		Assert.Equal("Correct feedback",      fb.CorrectAnswerFeedback);
		Assert.False(fb.IsCorrect);
	}

	[Fact]
	public void Test_Grade_CorrectAnswer_FeedbackFields_Valid()
	{
		var (test, correctId, _) = BuildTestWithFeedback();
		var tqId   = test.Questions[0].Id;
		var result = ScoreService.Grade(test, new[] { (tqId, (int?)correctId) });

		var fb = result.Feedback[0];
		Assert.Equal("Question-level hint", fb.QuestionFeedbackText);
		Assert.Equal("Correct feedback",    fb.SelectedAnswerFeedback); // selected = correct
		Assert.Equal("Correct feedback",    fb.CorrectAnswerFeedback);
		Assert.True(fb.IsCorrect);
	}

	[Fact]
	public void Test_Grade_Skipped_SelectedFeedbackIsNull_Valid()
	{
		var (test, _, _) = BuildTestWithFeedback();
		var tqId   = test.Questions[0].Id;
		var result = ScoreService.Grade(test, new[] { (tqId, (int?)null) });

		var fb = result.Feedback[0];
		Assert.Null(fb.SelectedAnswerFeedback); // nothing selected
		Assert.Equal("Correct feedback",      fb.CorrectAnswerFeedback);
		Assert.Equal("Question-level hint",   fb.QuestionFeedbackText);
	}

	[Fact]
	public void Test_Grade_NoFeedbackOnAnswers_FieldsAreNull_Valid()
	{
		var (test, _, wrongId) = BuildTest(); // base test has no feedback
		var tqId   = test.Questions[0].Id;
		var result = ScoreService.Grade(test, new[] { (tqId, (int?)wrongId) });

		var fb = result.Feedback[0];
		Assert.Null(fb.QuestionFeedbackText);
		Assert.Null(fb.SelectedAnswerFeedback);
		Assert.Null(fb.CorrectAnswerFeedback);
	}

	// GradeFromLetters

	[Theory]
	[InlineData("C", true,  1)] // slot C = correct
	[InlineData("A", false, 0)] // slot A = wrong
	[InlineData("B", false, 0)] // slot B = wrong
	[InlineData("D", false, 0)] // slot D = wrong
	public void Test_GradeFromLetters_Valid(string letter, bool isCorrect, int correctCount)
	{
		var (test, _, _) = BuildTest();
		var result = ScoreService.GradeFromLetters(test, letter);

		Assert.Equal(correctCount, result.CorrectCount);
		Assert.Equal(isCorrect, result.Answers[0].IsCorrect);
	}

	// ComputeDisplayScore

	[Theory]
	[InlineData(1, 100.0)]
	[InlineData(0, 0.0)]
	public void Test_ComputeDisplayScore_SimplePercent_Valid(int correctCount, double expectedPct)
	{
		var (test, _, _) = BuildTest();
		var strategy = new SimplePercentScoring();
		var score    = ScoreService.ComputeDisplayScore(test, correctCount, strategy);

		Assert.Equal(expectedPct, score, precision: 1);
	}
}
