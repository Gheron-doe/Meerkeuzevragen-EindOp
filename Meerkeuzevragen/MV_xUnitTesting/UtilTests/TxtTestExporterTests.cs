using MV_BL.Domain;
using MV_Util.Implementations;

namespace MV_xUnitTesting.UtilTests;

public class TxtTestExporterTests
{
    // Build a minimal 2-question test with deterministic answer sets.
    private static Test BuildTest()
    {
        var q1 = new Question(1, "What is 2+2?", 1);
        q1.Answers.Add(new Answer(1, 1, "4",  true));
        q1.Answers.Add(new Answer(2, 1, "3",  false));

        var q2 = new Question(2, "Capital of France?", 2);
        q2.Answers.Add(new Answer(3, 2, "Paris",  true));
        q2.Answers.Add(new Answer(4, 2, "Berlin", false));

        var tqs = new List<TestQuestion>
        {
            new TestQuestion(10, 1, 1, 1, q1),
            new TestQuestion(20, 1, 2, 2, q2)
        };
        return new Test(1, "My Test", DateTime.Now, false, tqs, new List<Topic>());
    }

    [Fact]
    public void Export_CreatesFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
        new TxtTestExporter().Export(BuildTest(), path);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Export_ContainsTitleLine()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
        new TxtTestExporter().Export(BuildTest(), path);
        string content = File.ReadAllText(path);
        Assert.Contains("My Test", content);
    }

    [Fact]
    public void Export_ContainsBothQuestionTexts()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
        new TxtTestExporter().Export(BuildTest(), path);
        string content = File.ReadAllText(path);
        Assert.Contains("What is 2+2?", content);
        Assert.Contains("Capital of France?", content);
    }

    [Fact]
    public void Export_WithAnswers_ContainsAnswerKeySection()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
        new TxtTestExporter().Export(BuildTest(), path, includeAnswers: true);
        string content = File.ReadAllText(path);
        Assert.Contains("Antwoorden", content);
    }

    [Fact]
    public void Export_WithoutAnswers_DoesNotContainAnswerKeySection()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
        new TxtTestExporter().Export(BuildTest(), path, includeAnswers: false);
        string content = File.ReadAllText(path);
        Assert.DoesNotContain("Antwoorden", content);
    }

    [Fact]
    public void Export_AnswerLabelsPresent()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
        new TxtTestExporter().Export(BuildTest(), path);
        string content = File.ReadAllText(path);
        // Each question should have at least "A." and "B." labels
        Assert.Matches(@"A\.", content);
        Assert.Matches(@"B\.", content);
    }

    [Fact]
    public void Format_IsTxt()
        => Assert.Equal("txt", new TxtTestExporter().Format);
}
