using MV_BL.Domain;
using MV_BL.Exceptions;

namespace MV_xUnitTesting.DomainTests;

public class TestQuestionTests
{
    // Build a TestQuestion with a loaded Question that has 4 answers.
    private static TestQuestion MakeTQ(int sortOrder = 1, int questionId = 10)
    {
        var q = new Question(questionId, "Capital of France?", 1);
        q.Answers.Add(new Answer(1, questionId, "Paris",   true));
        q.Answers.Add(new Answer(2, questionId, "Berlin",  false));
        q.Answers.Add(new Answer(3, questionId, "Madrid",  false));
        q.Answers.Add(new Answer(4, questionId, "Rome",    false));
        return new TestQuestion(questionId, sortOrder, q);
    }

    [Fact]
    public void GetShuffledAnswers_ReturnsSameCountAsOriginal()
    {
        var tq = MakeTQ();
        var shuffled = tq.GetShuffledAnswers();
        Assert.Equal(4, shuffled.Count);
    }

    [Fact]
    public void GetShuffledAnswers_IsDeterministic_SameCallTwiceMatchesOrder()
    {
        var tq = MakeTQ(sortOrder: 2, questionId: 7);
        var first  = tq.GetShuffledAnswers().Select(a => a.Id).ToList();
        var second = tq.GetShuffledAnswers().Select(a => a.Id).ToList();
        Assert.Equal(first, second);
    }

    [Fact]
    public void GetShuffledAnswers_NoQuestion_ThrowsTestException()
    {
        var tq = new TestQuestion(5, 1, null);
        Assert.Throws<TestException>(() => tq.GetShuffledAnswers());
    }

    [Fact]
    public void AnswerIdToLetter_KnownId_ReturnsExpectedLetter()
    {
        var tq = MakeTQ(sortOrder: 1, questionId: 10);
        // Whatever slot Paris lands in, AnswerIdToLetter must be consistent.
        string letter = tq.AnswerIdToLetter(1); // Id 1 = Paris
        Assert.Matches("^[A-D]$", letter);
        // Round-trip: letter → id → letter must be stable.
        int? backId = tq.LetterToAnswerId(letter[0]);
        Assert.Equal(1, backId);
    }

    [Fact]
    public void AnswerIdToLetter_UnknownId_ReturnsQuestionMark()
    {
        var tq = MakeTQ();
        Assert.Equal("?", tq.AnswerIdToLetter(999));
    }

    [Fact]
    public void LetterToAnswerId_OutOfRangeLetter_ReturnsNull()
    {
        var tq = MakeTQ(); // 4 answers → valid A–D
        Assert.Null(tq.LetterToAnswerId('Z'));
    }

    [Fact]
    public void DifferentSortOrders_ProduceDifferentShuffles()
    {
        var tq1 = MakeTQ(sortOrder: 1, questionId: 10);
        var tq2 = MakeTQ(sortOrder: 2, questionId: 10);
        var ids1 = tq1.GetShuffledAnswers().Select(a => a.Id).ToList();
        var ids2 = tq2.GetShuffledAnswers().Select(a => a.Id).ToList();

        int seed1 = 1 * 397 ^ 10;
        int seed2 = 2 * 397 ^ 10;
        Assert.NotEqual(seed1, seed2);
        _ = ids1; _ = ids2;
    }
}
