using MV_BL.Domain;
using MV_BL.Exceptions;

namespace MV_xUnitTesting.DomainTests;

public class QuestionTests
{
    // helper
    private static Question MakeValid(string text = "What is 2+2?", int diff = 1)
    {
        var q = new Question(text, diff);
        q.Answers.Add(new Answer("4", true));
        q.Answers.Add(new Answer("3", false));
        return q;
    }

    // setters

    [Fact]
    public void Id_Negative_ThrowsQuestionException()
    {
        var q = MakeValid();
        Assert.Throws<QuestionException>(() => q.Id = -1);
    }

    [Fact]
    public void QuestionText_Empty_ThrowsQuestionException()
        => Assert.Throws<QuestionException>(() => new Question("", 1));

    [Fact]
    public void QuestionText_IsTrimmed()
    {
        var q = new Question("  Hello  ", 1);
        Assert.Equal("Hello", q.QuestionText);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void DifficultyLevel_OutOfRange_ThrowsQuestionException(int bad)
        => Assert.Throws<QuestionException>(() => new Question("Text", bad));

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void DifficultyLevel_ValidValues_DoNotThrow(int valid)
    {
        var q = new Question("Text", valid);
        Assert.Equal(valid, q.DifficultyLevel);
    }

    // Answers setter validation

    [Fact]
    public void Answers_SetLessThanTwo_ThrowsQuestionException()
    {
        var q = new Question("Q?", 1);
        Assert.Throws<QuestionException>(() =>
            q.Answers = new List<Answer> { new Answer("Only one", true) });
    }

    [Fact]
    public void Answers_SetNoCorrectAnswer_ThrowsAnswerException()
    {
        var q = new Question("Q?", 1);
        Assert.Throws<AnswerException>(() =>
            q.Answers = new List<Answer> { new Answer("A", false), new Answer("B", false) });
    }

    [Fact]
    public void Answers_SetTwoCorrectAnswers_ThrowsAnswerException()
    {
        var q = new Question("Q?", 1);
        Assert.Throws<AnswerException>(() =>
            q.Answers = new List<Answer> { new Answer("A", true), new Answer("B", true) });
    }

    [Fact]
    public void Answers_SetExactlyOneCorrect_DoesNotThrow()
    {
        var q = new Question("Q?", 1);
        var ex = Record.Exception(() =>
            q.Answers = new List<Answer> { new Answer("A", true), new Answer("B", false) });
        Assert.Null(ex);
    }

    // IsActive

    [Fact]
    public void IsActive_DefaultsToTrue()
    {
        var q = new Question("Text?", 1);
        Assert.True(q.IsActive);
    }

    [Fact]
    public void IsActive_CanBeSetToFalse()
    {
        var q = new Question("Text?", 1);
        q.IsActive = false;
        Assert.False(q.IsActive);
    }

    [Fact]
    public void Generate_InactiveQuestion_IsExcluded()
    {
        // pool of 5, all inactive
        var pool = Enumerable.Range(1, 5).Select(i =>
        {
            var q = new Question(i, $"Q{i}?", 1);
            q.Answers.Add(new Answer(i * 10,     i, "Right", true));
            q.Answers.Add(new Answer(i * 10 + 1, i, "Wrong", false));
            q.IsActive = false;
            return q;
        }).ToList();

        Assert.Throws<MV_BL.Exceptions.TestException>(
            () => MV_BL.Domain.Test.Generate("T", pool, 3, 0, 0, 0));
    }

    [Fact]
    public void Answers_SetAnswerWithWrongQuestionId_ThrowsAnswerException()
    {
        var q = new Question(1, "Q?", 1);
        Assert.Throws<AnswerException>(() =>
            q.Answers = new List<Answer>
            {
                new Answer(1, 1, "Right", true),
                new Answer(99, 999, "Wrong FK", false)
            });
    }
}
