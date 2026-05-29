using MV_BL.Domain;
using MV_BL.Exceptions;

namespace MV_xUnitTesting.DomainTests;

public class TestAttemptTests
{
    // Validate() 

    [Fact]
    public void Validate_ZeroTestId_ThrowsTestException()
    {
        var a = new TestAttempt(0, 1);
        Assert.Throws<TestException>(() => a.Validate());
    }

    [Fact]
    public void Validate_ZeroUserId_ThrowsUserException()
    {
        var a = new TestAttempt(1, 0);
        Assert.Throws<UserException>(() => a.Validate());
    }

    [Fact]
    public void Validate_ValidIds_DoesNotThrow()
    {
        var a = new TestAttempt(1, 2);
        var ex = Record.Exception(() => a.Validate());
        Assert.Null(ex);
    }

    // Setter validation 

    [Fact]
    public void TestId_NegativeValue_ThrowsTestException()
    {
        var a = new TestAttempt(1, 1, 1, DateTime.Now);
        Assert.Throws<TestException>(() => a.TestId = -1);
    }

    [Fact]
    public void UserId_NegativeValue_ThrowsUserException()
    {
        var a = new TestAttempt(1, 1, 1, DateTime.Now);
        Assert.Throws<UserException>(() => a.UserId = -1);
    }

    [Fact]
    public void TestId_ZeroIsValid()
    {
        var a = new TestAttempt(1, 1, 1, DateTime.Now);
        a.TestId = 0;
        Assert.Equal(0, a.TestId);
    }

    [Fact]
    public void UserId_ZeroIsValid()
    {
        var a = new TestAttempt(1, 1, 1, DateTime.Now);
        a.UserId = 0;
        Assert.Equal(0, a.UserId);
    }

    // CountCorrect / PercentScore

    [Fact]
    public void CountCorrect_AllRight_ReturnsQuestionCount()
    {
        var (test, attempt) = BuildScenario(correctMask: new[] { true, true, true });
        Assert.Equal(3, attempt.CountCorrect(test));
    }

    [Fact]
    public void CountCorrect_AllWrong_ReturnsZero()
    {
        var (test, attempt) = BuildScenario(correctMask: new[] { false, false, false });
        Assert.Equal(0, attempt.CountCorrect(test));
    }

    [Fact]
    public void CountCorrect_Mixed_ReturnsPartialCount()
    {
        var (test, attempt) = BuildScenario(correctMask: new[] { true, false, true });
        Assert.Equal(2, attempt.CountCorrect(test));
    }

    [Fact]
    public void PercentScore_AllRight_Returns100()
    {
        var (test, attempt) = BuildScenario(correctMask: new[] { true, true });
        Assert.Equal(100.0, attempt.PercentScore(test));
    }

    [Fact]
    public void PercentScore_OneOfTwo_Returns50()
    {
        var (test, attempt) = BuildScenario(correctMask: new[] { true, false });
        Assert.Equal(50.0, attempt.PercentScore(test));
    }

    [Fact]
    public void PercentScore_NoQuestions_ReturnsZero()
    {
        var test = new Test("T", new List<TestQuestion>(), new List<Topic>());
        var attempt = new TestAttempt(1, 1, 1, DateTime.Now, DateTime.Now);
        Assert.Equal(0.0, attempt.PercentScore(test));
    }

    // ToLetterString / AnswersFromLetters

    [Fact]
    public void AnswersFromLetters_ThenToLetterString_RoundTripsCorrectly()
    {
        var test = BuildTest(questionCount: 3);
        int attemptId = 1;
        string original = "ABА";

        var answers = TestAttempt.AnswersFromLetters(attemptId, test, "AAA");
        var attempt = new TestAttempt(attemptId, 1, 1, DateTime.Now, DateTime.Now,
            null, answers);

        string result = attempt.ToLetterString(test);
        Assert.Equal(3, result.Length);
        Assert.All(result.ToCharArray(), c => Assert.Matches("[A-D?]", c.ToString()));
    }

    [Fact]
    public void ToLetterString_SkippedQuestions_MarkedWithQuestionMark()
    {
        var test = BuildTest(questionCount: 2);

        var answers = test.Questions.OrderBy(tq => tq.SortOrder)
            .Select(tq => new AttemptAnswer(1, tq.Id, null))
            .ToList();
        var attempt = new TestAttempt(1, 1, 1, DateTime.Now, DateTime.Now, null, answers);
        Assert.Equal("??", attempt.ToLetterString(test));
    }

    //  helpers

    // Test + TestAttempt  each question has one correct and one wrong answer.
    private static (Test test, TestAttempt attempt) BuildScenario(bool[] correctMask)
    {
        var tqs = new List<TestQuestion>();
        var answers = new List<AttemptAnswer>();
        int nextId = 1;

        for (int i = 0; i < correctMask.Length; i++)
        {
            int qId   = nextId++;
            int aCorr = nextId++;
            int aWrong= nextId++;
            int tqId  = nextId++;

            var q = new Question(qId, $"Q{i}?", 1);
            q.Answers.Add(new Answer(aCorr,  qId, "Correct", true));
            q.Answers.Add(new Answer(aWrong, qId, "Wrong",   false));

            var tq = new TestQuestion(tqId, 1, qId, i + 1, q);
            tqs.Add(tq);

            int chosen = correctMask[i] ? aCorr : aWrong;
            answers.Add(new AttemptAnswer(1, tqId, chosen));
        }

        var test    = new Test(1, "T", DateTime.Now, false, tqs, new List<Topic>());
        var attempt = new TestAttempt(1, 1, 1, DateTime.Now, DateTime.Now, null, answers);
        return (test, attempt);
    }

    private static Test BuildTest(int questionCount)
    {
        var tqs = new List<TestQuestion>();
        for (int i = 0; i < questionCount; i++)
        {
            int qId = i + 1;
            var q = new Question(qId, $"Q{i}?", 1);
            q.Answers.Add(new Answer(qId * 10,     qId, "A", true));
            q.Answers.Add(new Answer(qId * 10 + 1, qId, "B", false));
            tqs.Add(new TestQuestion(qId * 100, 1, qId, i + 1, q));
        }
        return new Test(1, "T", DateTime.Now, false, tqs, new List<Topic>());
    }
}
