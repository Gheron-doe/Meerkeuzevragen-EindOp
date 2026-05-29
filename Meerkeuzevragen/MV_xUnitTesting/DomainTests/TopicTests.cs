using MV_BL.Domain;
using MV_BL.Exceptions;

namespace MV_xUnitTesting.DomainTests;

public class TopicTests
{
    [Fact]
    public void FullCtor_SetsProperties()
    {
        var t = new Topic(3, "History", false);
        Assert.Equal(3, t.Id);
        Assert.Equal("History", t.Name);
        Assert.False(t.IsFlagged);
    }

    [Fact]
    public void MinimalCtor_IdZeroNotFlagged()
    {
        var t = new Topic("Science");
        Assert.Equal(0, t.Id);
        Assert.False(t.IsFlagged);
    }

    [Fact]
    public void Id_NegativeValue_ThrowsTopicException()
    {
        var t = new Topic("Math");
        Assert.Throws<TopicException>(() => t.Id = -5);
    }

    [Fact]
    public void Name_Empty_ThrowsTopicException()
        => Assert.Throws<TopicException>(() => new Topic(""));

    [Fact]
    public void Name_IsTrimmedOnAssignment()
    {
        var t = new Topic("  Art  ");
        Assert.Equal("Art", t.Name);
    }

    [Fact]
    public void IsFlagged_CanBeSetToTrue()
    {
        var t = new Topic("PE");
        t.IsFlagged = true;
        Assert.True(t.IsFlagged);
    }
}
