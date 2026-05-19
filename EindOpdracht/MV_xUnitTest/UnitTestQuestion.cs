using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Services;
using MV_xUnitTest.Fakes;

namespace MV_xUnitTest;

public class UnitTestQuestion
{
	// Setup

	private static (QuestionService svc, FakeQuestionRepository qRepo, FakeTopicRepository tRepo, int topicId) Setup()
	{
		var qRepo   = new FakeQuestionRepository();
		var tRepo   = new FakeTopicRepository();
		var topicId = tRepo.Add(new Topic { Name = "SQL" });
		return (new QuestionService(qRepo, tRepo), qRepo, tRepo, topicId);
	}

	private static (string text, bool isCorrect, string? feedback)[] ValidAnswers() =>
		new[] { ("Correct", true, null), ("Wrong", false, (string?)null) };

	//  AddQuestion: QuestionText

	[Theory]
	[InlineData("Wat is SQL?")]
	[InlineData("Q")]
	[InlineData("Een zin met meerdere woorden")]
	public void Test_AddQuestion_Text_Valid(string text)
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id = svc.AddQuestion(topicId, text, 1, ValidAnswers());

		Assert.True(id > 0);
		Assert.Equal(text.Trim(), qRepo.GetById(id)!.QuestionText);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("   ")]
	public void Test_AddQuestion_Text_Invalid(string text)
	{
		var (svc, _, _, topicId) = Setup();
		Assert.Throws<QuestionValidationException>(() =>
			svc.AddQuestion(topicId, text, 1, ValidAnswers()));
	}

	// AddQuestion: DifficultyLevel 

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void Test_AddQuestion_Difficulty_Valid(int difficulty)
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id = svc.AddQuestion(topicId, "Q?", difficulty, ValidAnswers());

		Assert.Equal(difficulty, qRepo.GetById(id)!.DifficultyLevel);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(4)]
	[InlineData(-1)]
	public void Test_AddQuestion_Difficulty_Invalid(int difficulty)
	{
		var (svc, _, _, topicId) = Setup();
		Assert.Throws<InvalidDifficultyException>(() =>
			svc.AddQuestion(topicId, "Q?", difficulty, ValidAnswers()));
	}

	// AddQuestion: TopicId

	[Theory]
	[InlineData(999)]
	[InlineData(-1)]
	[InlineData(0)]
	public void Test_AddQuestion_TopicId_Invalid(int badTopicId)
	{
		var (svc, _, _, _) = Setup();
		Assert.Throws<TopicNotFoundException>(() =>
			svc.AddQuestion(badTopicId, "Q?", 1, ValidAnswers()));
	}

	// AddQuestion: Answers

	[Fact]
	public void Test_AddQuestion_Answers_Valid()
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id = svc.AddQuestion(topicId, "Q?", 1, new[]
		{
			("A", true,  null),
			("B", false, null),
			("C", false, null),
			("D", false, (string?)null)
		});

		var q = qRepo.GetById(id)!;
		Assert.Equal(4, q.Answers.Count);
		Assert.Single(q.Answers, a => a.IsCorrect);
	}

	[Fact]
	public void Test_AddQuestion_Answers_Invalid_NoCorrect()
	{
		var (svc, _, _, topicId) = Setup();
		Assert.Throws<QuestionValidationException>(() =>
			svc.AddQuestion(topicId, "Q?", 1, new[]
			{
				("A", false, null),
				("B", false, (string?)null)
			}));
	}

	[Fact]
	public void Test_AddQuestion_Answers_Invalid_MultipleCorrect()
	{
		var (svc, _, _, topicId) = Setup();
		Assert.Throws<QuestionValidationException>(() =>
			svc.AddQuestion(topicId, "Q?", 1, new[]
			{
				("A", true, null),
				("B", true, (string?)null)
			}));
	}

	[Fact]
	public void Test_AddQuestion_Answers_Invalid_TooFew()
	{
		var (svc, _, _, topicId) = Setup();
		Assert.Throws<QuestionValidationException>(() =>
			svc.AddQuestion(topicId, "Q?", 1, new[]
			{
				("Only", true, (string?)null)
			}));
	}

	[Fact]
	public void Test_AddQuestion_Answers_Invalid_BlankText()
	{
		var (svc, _, _, topicId) = Setup();
		Assert.Throws<QuestionValidationException>(() =>
			svc.AddQuestion(topicId, "Q?", 1, new[]
			{
				("",    true,  null),
				("B",   false, (string?)null)
			}));
	}

	// AddQuestion: Feedback (optional)

	[Fact]
	public void Test_AddQuestion_Feedback_Valid()
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id = svc.AddQuestion(topicId, "Q?", 1,
			new[] { ("Correct", true, (string?)"Good!"), ("Wrong", false, (string?)"Nope") },
			feedback: "Hint text");

		var q = qRepo.GetById(id)!;
		Assert.Equal("Hint text", q.Feedback);
		Assert.Equal("Good!", q.Answers.First(a =>  a.IsCorrect).Feedback);
		Assert.Equal("Nope",  q.Answers.First(a => !a.IsCorrect).Feedback);
	}

	[Fact]
	public void Test_AddQuestion_Feedback_Null_Valid()
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id = svc.AddQuestion(topicId, "Q?", 1, ValidAnswers(), feedback: null);

		Assert.Null(qRepo.GetById(id)!.Feedback);
	}

	// AddQuestion: initial state

	[Fact]
	public void Test_AddQuestion_InitialState_Valid()
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id = svc.AddQuestion(topicId, "Q?", 1, ValidAnswers());
		var q  = qRepo.GetById(id)!;

		Assert.True(q.IsActive);
		Assert.False(q.IsFlagged);
	}

	// UpdateQuestion: QuestionText

	[Theory]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("  ")]
	public void Test_UpdateQuestion_Text_Invalid(string text)
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id      = svc.AddQuestion(topicId, "Original?", 1, ValidAnswers());
		var existing = qRepo.GetById(id)!;

		Assert.Throws<QuestionValidationException>(() =>
			svc.UpdateQuestion(id, text, 1, new[]
			{
				(existing.Answers[0].Id, "Correct", true,  (string?)null),
				(existing.Answers[1].Id, "Wrong",   false, (string?)null)
			}));
	}

	// UpdateQuestion: DifficultyLevel

	[Theory]
	[InlineData(0)]
	[InlineData(4)]
	[InlineData(5)]
	public void Test_UpdateQuestion_Difficulty_Invalid(int difficulty)
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id       = svc.AddQuestion(topicId, "Q?", 1, ValidAnswers());
		var existing = qRepo.GetById(id)!;

		Assert.Throws<InvalidDifficultyException>(() =>
			svc.UpdateQuestion(id, "Q?", difficulty, new[]
			{
				(existing.Answers[0].Id, "Correct", true,  (string?)null),
				(existing.Answers[1].Id, "Wrong",   false, (string?)null)
			}));
	}

	[Theory]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public void Test_UpdateQuestion_Difficulty_Valid(int difficulty)
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id       = svc.AddQuestion(topicId, "Q?", 1, ValidAnswers());
		var existing = qRepo.GetById(id)!;

		svc.UpdateQuestion(id, "Updated?", difficulty, new[]
		{
			(existing.Answers[0].Id, "Correct", true,  (string?)null),
			(existing.Answers[1].Id, "Wrong",   false, (string?)null)
		});

		Assert.Equal(difficulty, qRepo.GetById(id)!.DifficultyLevel);
	}

	// UpdateQuestion: blank new answer slots stripped

	[Fact]
	public void Test_UpdateQuestion_BlankNewAnswers_Stripped_Valid()
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id       = svc.AddQuestion(topicId, "Q?", 1, ValidAnswers());
		var existing = qRepo.GetById(id)!;

		svc.UpdateQuestion(id, "Updated?", 2, new[]
		{
			(existing.Answers[0].Id, "Correct", true,  (string?)null),
			(existing.Answers[1].Id, "Wrong",   false, (string?)null),
			(0, "New valid",  false, (string?)null),
			(0, "",           false, (string?)null),   // blank → stripped
			(0, "   ",        false, (string?)null)    // whitespace → stripped
		});

		Assert.Equal(3, qRepo.GetById(id)!.Answers.Count);
	}

	// Deactivate

	[Fact]
	public void Test_Deactivate_Valid()
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id = svc.AddQuestion(topicId, "Q?", 1, ValidAnswers());
		svc.Deactivate(id);

		var q = qRepo.GetById(id)!;
		Assert.False(q.IsActive);
		Assert.False(q.IsFlagged); // Deactivate must NOT set IsFlagged
	}

	// Activate

	[Fact]
	public void Test_Activate_Valid()
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id = svc.AddQuestion(topicId, "Q?", 1, ValidAnswers());
		svc.Deactivate(id);
		svc.Activate(id);

		var q = qRepo.GetById(id)!;
		Assert.True(q.IsActive);
		Assert.False(q.IsFlagged); // Activate must NOT set IsFlagged
	}

	// Flag (soft-delete)

	[Fact]
	public void Test_Flag_Valid()
	{
		var (svc, qRepo, _, topicId) = Setup();
		var id = svc.AddQuestion(topicId, "Q?", 1, ValidAnswers());
		svc.Flag(id);

		Assert.True(qRepo.GetById(id)!.IsFlagged);
	}

	[Fact]
	public void Test_Flag_HidesFromGetAll_Valid()
	{
		var (svc, _, _, topicId) = Setup();
		svc.AddQuestion(topicId, "Q?", 1, ValidAnswers());
		var id = svc.AddQuestion(topicId, "Q2?", 1, ValidAnswers());
		svc.Flag(id);

		Assert.Single(svc.GetAll()); // only 1 of 2 visible
	}

	// GetAll

	[Fact]
	public void Test_GetAll_Valid()
	{
		var (svc, _, tRepo, topicId) = Setup();
		var topic2 = tRepo.Add(new Topic { Name = "Math" });

		svc.AddQuestion(topicId, "SQL Q?",  1, ValidAnswers());
		svc.AddQuestion(topic2,  "Math Q?", 1, ValidAnswers());

		Assert.Equal(2, svc.GetAll().Count);
	}

	[Fact]
	public void Test_GetAll_Empty_Valid()
	{
		var (svc, _, _, _) = Setup();
		Assert.Empty(svc.GetAll());
	}

	// GetByTopic

	[Fact]
	public void Test_GetByTopic_Valid()
	{
		var (svc, _, _, topicId) = Setup();
		svc.AddQuestion(topicId, "Q1?", 1, ValidAnswers());
		svc.AddQuestion(topicId, "Q2?", 2, ValidAnswers());

		Assert.Equal(2, svc.GetByTopic(topicId).Count);
	}

	[Fact]
	public void Test_GetByTopic_Invalid()
	{
		var (svc, _, _, _) = Setup();
		Assert.Throws<TopicNotFoundException>(() => svc.GetByTopic(999));
	}
}
