using MV_BL.Domain;
using MV_BL.Exceptions;

namespace MV_xUnitTesting.DomainTests;

public class TestDomainTests
{
    // Id / Title setters

    [Fact]
    public void Id_Negative_ThrowsTestException()
    {
        var t = new Test("T", new List<TestQuestion>(), new List<Topic>());
        Assert.Throws<TestException>(() => t.Id = -1);
    }

    [Fact]
    public void Title_Empty_ThrowsTestException()
        => Assert.Throws<TestException>(
            () => new Test("", new List<TestQuestion>(), new List<Topic>()));

    [Fact]
    public void Title_IsTrimmed()
    {
        var t = new Test("  My Test  ", new List<TestQuestion>(), new List<Topic>());
        Assert.Equal("My Test", t.Title);
    }

    // Validate() 

    [Fact]
    public void Validate_EmptyQuestionList_ThrowsTestException()
    {
        var t = new Test("Empty", new List<TestQuestion>(), new List<Topic>());
        Assert.Throws<TestException>(() => t.Validate());
    }

    [Fact]
    public void Validate_WithQuestions_DoesNotThrow()
    {
        var q  = new Question(1, "Q?", 1);
        var tq = new TestQuestion(1, 1, q);   // (questionId, sortOrder, question)
        var t  = new Test("Has Qs", new List<TestQuestion> { tq }, new List<Topic>());
        var ex = Record.Exception(() => t.Validate());
        Assert.Null(ex);
    }

    // Generate()

    [Fact]
    public void Generate_EmptyTitle_ThrowsTestException()
    {
        var pool = BuildPool(5);
        Assert.Throws<TestException>(() => Test.Generate("", pool, 3, 0, 0, 0));
    }

    [Fact]
    public void Generate_EmptyPool_ThrowsTestException()
        => Assert.Throws<TestException>(() =>
            Test.Generate("T", new List<Question>(), 3, 0, 0, 0));

    [Fact]
    public void Generate_FlatCount_ProducesCorrectNumberOfQuestions()
    {
        var pool = BuildPool(10);
        var t = Test.Generate("T", pool, 5, 0, 0, 0);
        Assert.Equal(5, t.Questions.Count);
    }

    [Fact]
    public void Generate_PerDifficulty_PicksFromEachLevel()
    {
        var pool = BuildPool(6, diff1: 2, diff2: 2, diff3: 2);
        var t = Test.Generate("T", pool, 0, 1, 1, 1);
        Assert.Equal(3, t.Questions.Count);
    }

    [Fact]
    public void Generate_FlaggedQuestionsExcluded()
    {
        var pool = BuildPool(5);
        foreach (var q in pool) q.IsFlagged = true;
        Assert.Throws<TestException>(() => Test.Generate("T", pool, 3, 0, 0, 0));
    }

    [Fact]
    public void Generate_ZeroCount_ThrowsTestException()
    {
        var pool = BuildPool(5);
        Assert.Throws<TestException>(() => Test.Generate("T", pool, 0, 0, 0, 0));
    }

    [Fact]
    public void Generate_ResultQuestionsHaveUniqueSortOrders()
    {
        var pool = BuildPool(5);
        var t = Test.Generate("T", pool, 4, 0, 0, 0);
        var sorts = t.Questions.Select(tq => tq.SortOrder).ToList();
        Assert.Equal(sorts.Count, sorts.Distinct().Count());
    }

    // DifficultiesString / TopicsString

    [Fact]
    public void DifficultiesString_ReturnsCommaSeparatedSortedLevels()
    {
        var q1 = MakeQ(1, 1);
        var q3 = MakeQ(2, 3);
        var tqs = new List<TestQuestion>
        {
            new TestQuestion(1, 1, q1),
            new TestQuestion(2, 2, q3)
        };
        var t = new Test("T", tqs, new List<Topic>());
        Assert.Equal("1,3", t.DifficultiesString());
    }

    [Fact]
    public void TopicsString_ReturnsCommaJoinedTopicNames()
    {
        var topics = new List<Topic>
        {
            new Topic(1, "History", false),
            new Topic(2, "Science", false)
        };
        var t = new Test("T", new List<TestQuestion>(), topics);
        Assert.Equal("History, Science", t.TopicsString());
    }

    [Fact]
    public void TopicsString_EmptyTopics_ReturnsEmptyString()
    {
        var t = new Test("T", new List<TestQuestion>(), new List<Topic>());
        Assert.Equal(string.Empty, t.TopicsString());
    }

    // helpers

    private static List<Question> BuildPool(int count,
        int diff1 = 0, int diff2 = 0, int diff3 = 0)
    {
        var list = new List<Question>();
        int id = 1;
        for (int i = 0; i < diff1; i++) list.Add(MakeQ(id++, 1));
        for (int i = 0; i < diff2; i++) list.Add(MakeQ(id++, 2));
        for (int i = 0; i < diff3; i++) list.Add(MakeQ(id++, 3));
        while (list.Count < count) list.Add(MakeQ(id++, 1));
        return list;
    }

    private static Question MakeQ(int id, int diff)
    {
        var q = new Question(id, $"Q{id}?", diff);
        q.Answers.Add(new Answer(id * 10,     id, "Right", true));
        q.Answers.Add(new Answer(id * 10 + 1, id, "Wrong", false));
        return q;
    }
}
