using MV_BL.Domain;
using MV_BL.Interfaces;
using MV_BL.Services;
using MV_xUnitTest.Fakes;

namespace MV_xUnitTest;


public class UnitTestAttempt
{
	// Setup 

	private static (AttemptService svc, FakeAttemptRepository aRepo, FakeUserRepository uRepo, Test test) Setup()
	{
		var aRepo = new FakeAttemptRepository();
		var tRepo = new FakeTestRepository();
		var uRepo = new FakeUserRepository();

		var correct = new Answer { Id = 1, AnswerText = "Right", IsCorrect = true,  OriginalOrder = 0 };
		var wrong   = new Answer { Id = 2, AnswerText = "Wrong", IsCorrect = false, OriginalOrder = 1 };

		var q = new Question
		{
			Id = 10, TopicId = 1, QuestionText = "Q?",
			Answers = new List<Answer> { correct, wrong }
		};

		var tq = new TestQuestion
		{
			Id = 100, QuestionId = 10, QuestionOrder = 1,
			AnswerDisplayOrder = new List<int> { 0, 1 }, // slot A = correct
			Question = q
		};

		var test = new Test { Id = 0, TopicId = 1, Title = "T", Questions = new List<TestQuestion> { tq } };
		int testId = tRepo.Add(test);
		tq.TestId  = testId;
		test       = tRepo.GetById(testId)!;

		uRepo.Add(new User { Username = "alice" });

		return (new AttemptService(aRepo, tRepo, uRepo), aRepo, uRepo, test);
	}

	// StartAttempt 

	[Fact]
	public void Test_StartAttempt_KnownUser_Valid()
	{
		var (svc, aRepo, _, _) = Setup();
		var (attemptId, startedAt) = svc.StartAttempt(testId: 1, username: "alice");

		Assert.True(attemptId > 0);
		Assert.Single(aRepo.Store);
		Assert.Equal(attemptId, aRepo.Store[0].Id);
		Assert.Null(aRepo.Store[0].CompletedAt);
	}

	[Fact]
	public void Test_StartAttempt_NewUser_Valid()
	{
		var (svc, _, uRepo, _) = Setup();
		svc.StartAttempt(testId: 1, username: "bob"); // bob doesn't exist yet

		Assert.NotNull(uRepo.GetByUsername("bob")); // auto-created
	}

	// CompleteAttempt 

	[Fact]
	public void Test_CompleteAttempt_Correct_Valid()
	{
		var (svc, aRepo, _, test) = Setup();
		var (attemptId, _) = svc.StartAttempt(test.Id, "alice");
		var tqId           = test.Questions[0].Id;

		var (_, grading) = svc.CompleteAttempt(attemptId, test.Id,
			new[] { (tqId, (int?)1) }); // Id=1 is correct

		Assert.Equal(1, grading.CorrectCount);
		Assert.NotNull(aRepo.Store[0].CompletedAt);
		Assert.Equal(1, aRepo.Store[0].Score);
	}

	[Fact]
	public void Test_CompleteAttempt_Wrong_Valid()
	{
		var (svc, aRepo, _, test) = Setup();
		var (attemptId, _) = svc.StartAttempt(test.Id, "alice");
		var tqId           = test.Questions[0].Id;

		var (_, grading) = svc.CompleteAttempt(attemptId, test.Id,
			new[] { (tqId, (int?)2) }); // Id=2 is wrong

		Assert.Equal(0, grading.CorrectCount);
		Assert.Equal(0, aRepo.Store[0].Score);
	}

	[Fact]
	public void Test_CompleteAttempt_Skipped_Valid()
	{
		var (svc, aRepo, _, test) = Setup();
		var (attemptId, _) = svc.StartAttempt(test.Id, "alice");
		var tqId           = test.Questions[0].Id;

		var (_, grading) = svc.CompleteAttempt(attemptId, test.Id,
			new[] { (tqId, (int?)null) }); // null = skipped

		Assert.Equal(0, grading.CorrectCount);
		Assert.Equal(0, aRepo.Store[0].Score);
	}

	// SetFeedback 

	[Theory]
	[InlineData("Goed gedaan!")]
	[InlineData("Nog te verbeteren.")]
	[InlineData("ok")]
	public void Test_SetFeedback_Valid(string feedbackText)
	{
		var (svc, aRepo, _, test) = Setup();
		var (attemptId, _) = svc.StartAttempt(test.Id, "alice");
		svc.CompleteAttempt(attemptId, test.Id,
			new[] { (test.Questions[0].Id, (int?)1) });

		svc.SetFeedback(attemptId, feedbackText);

		Assert.Equal(feedbackText, aRepo.Store[0].Feedback);
	}

	// ImportFromFile: known user

	[Fact]
	public void Test_ImportFromFile_KnownUser_Valid()
	{
		var (svc, aRepo, _, test) = Setup();
		var rows   = new List<BulkRow> { new(UserId: 1, Answers: "A") }; // alice id=1
		var parser = new FakeBulkParser(rows);

		var results = svc.ImportFromFile(test.Id, "fake.csv", parser);

		Assert.Single(results);
		Assert.True(results[0].Persisted);
		Assert.Equal(1, results[0].CorrectCount);
		Assert.NotEmpty(aRepo.Store);
	}

	// ImportFromFile: unknown user

	[Theory]
	[InlineData(999)]
	[InlineData(0)]
	[InlineData(-1)]
	public void Test_ImportFromFile_UnknownUser_Invalid(int unknownId)
	{
		var (svc, aRepo, _, test) = Setup();
		var rows   = new List<BulkRow> { new(UserId: unknownId, Answers: "A") };
		var parser = new FakeBulkParser(rows);

		var results = svc.ImportFromFile(test.Id, "fake.csv", parser);

		Assert.False(results[0].Persisted); // unknown user → skipped
		Assert.Empty(aRepo.Store);          // nothing saved
	}

	// ImportFromFile: multiple rows

	[Fact]
	public void Test_ImportFromFile_MultipleRows_Valid()
	{
		var (svc, aRepo, uRepo, test) = Setup();
		uRepo.Add(new User { Username = "bob" }); // id=2
		var rows = new List<BulkRow>
		{
			new(UserId: 1, Answers: "A"), // alice, correct
			new(UserId: 2, Answers: "B")  // bob, wrong
		};

		var results = svc.ImportFromFile(test.Id, "fake.csv", new FakeBulkParser(rows));

		Assert.Equal(2, results.Count);
		Assert.Equal(2, aRepo.Store.Count);
		Assert.Equal(1, results[0].CorrectCount);
		Assert.Equal(0, results[1].CorrectCount);
	}

	// ImportFromFile: feedback column

	[Fact]
	public void Test_ImportFromFile_FeedbackColumn_Valid()
	{
		var (svc, aRepo, _, test) = Setup();
		var rows   = new List<BulkRow> { new(UserId: 1, Answers: "A", Feedback: "Goed werk") };
		var parser = new FakeBulkParser(rows);

		svc.ImportFromFile(test.Id, "fake.csv", parser);

		Assert.Equal("Goed werk", aRepo.Store[0].Feedback);
	}

	[Fact]
	public void Test_ImportFromFile_NullFeedbackColumn_Valid()
	{
		var (svc, aRepo, _, test) = Setup();
		var rows   = new List<BulkRow> { new(UserId: 1, Answers: "A", Feedback: null) };
		var parser = new FakeBulkParser(rows);

		svc.ImportFromFile(test.Id, "fake.csv", parser);

		Assert.Null(aRepo.Store[0].Feedback); // no feedback stays null
	}
}

// FakeBulkParser

public class FakeBulkParser : IBulkInputParser
{
	private readonly IEnumerable<BulkRow> _rows;
	public string Format => "csv";
	public FakeBulkParser(IEnumerable<BulkRow> rows) => _rows = rows;
	public IEnumerable<BulkRow> Parse(string filePath) => _rows;
}
