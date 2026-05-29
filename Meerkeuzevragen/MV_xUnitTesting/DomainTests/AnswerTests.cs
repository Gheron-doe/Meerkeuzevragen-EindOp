using MV_BL.Domain;
using MV_BL.Exceptions;

namespace MV_xUnitTesting.DomainTests;

public class AnswerTests
{
    // construction

    [Fact]
    public void FullCtor_SetsAllProperties()
    {
        var a = new Answer(1, 10, "Paris", true, "Capital of France");
        Assert.Equal(1, a.Id);
        Assert.Equal(10, a.QuestionId);
        Assert.Equal("Paris", a.AnswerText);
        Assert.True(a.IsCorrect);
        Assert.Equal("Capital of France", a.Feedback);
    }

    [Fact]
    public void MinimalCtor_DefaultsIdAndQuestionIdToZero()
    {
        var a = new Answer("Berlin", false);
        Assert.Equal(0, a.Id);
        Assert.Equal(0, a.QuestionId);
        Assert.False(a.IsCorrect);
    }

    // Id setter

    [Fact]
    public void Id_NegativeValue_ThrowsAnswerException()
    {
        var a = new Answer("text", false);
        Assert.Throws<AnswerException>(() => a.Id = -1);
    }

    [Fact]
    public void Id_ZeroIsValid()
    {
        var a = new Answer("text", false);
        a.Id = 0;   // should not throw
        Assert.Equal(0, a.Id);
    }

    // AnswerText setter

    [Fact]
    public void AnswerText_EmptyString_ThrowsAnswerException()
        => Assert.Throws<AnswerException>(() => new Answer("", false));

    [Fact]
    public void AnswerText_WhitespaceOnly_ThrowsAnswerException()
        => Assert.Throws<AnswerException>(() => new Answer("   ", false));

    [Fact]
    public void AnswerText_IsTrimmedOnAssignment()
    {
        var a = new Answer("  Rome  ", true);
        Assert.Equal("Rome", a.AnswerText);
    }
}
