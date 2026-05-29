using MV_BL.Exceptions;
using MV_Util.Implementations;

namespace MV_xUnitTesting.UtilTests;

public class TxtQuestionImporterTests
{
    private static string WriteTempTxt(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".txt");
        File.WriteAllText(path, content);
        return path;
    }

    // Minimal valid txt file: 2 questions, answer key has one letter each.
    private const string ValidTxt =
        "1. What is 2+2?\n" +
        "A. 3\n" +
        "B. 4\n" +
        "C. 5\n" +
        "\n" +
        "2. Capital of France?\n" +
        "A. Berlin\n" +
        "B. Paris\n" +
        "C. Madrid\n" +
        "\n" +
        "Antwoorden\n" +
        "B\n" +   // Q1 correct = B (4)
        "B\n";    // Q2 correct = B (Paris)

    [Fact]
    public void Import_ValidFile_ReturnsTwoQuestions()
    {
        var path = WriteTempTxt(ValidTxt);
        var questions = new TxtQuestionImporter().Import(path);
        Assert.Equal(2, questions.Count);
    }

    [Fact]
    public void Import_EachQuestion_HasExactlyOneCorrectAnswer()
    {
        var path = WriteTempTxt(ValidTxt);
        var questions = new TxtQuestionImporter().Import(path);
        foreach (var q in questions)
            Assert.Equal(1, q.Answers.Count(a => a.IsCorrect));
    }

    [Fact]
    public void Import_EachQuestion_HasExpectedAnswerCount()
    {
        var path = WriteTempTxt(ValidTxt);
        var questions = new TxtQuestionImporter().Import(path);
        Assert.All(questions, q => Assert.Equal(3, q.Answers.Count));
    }

    [Fact]
    public void Import_CorrectAnswerMatchesKeyLetter()
    {
        var path = WriteTempTxt(ValidTxt);
        var questions = new TxtQuestionImporter().Import(path);
        // Q1: correct should be "4" (B slot)
        var correctQ1 = questions[0].Answers.Single(a => a.IsCorrect);
        Assert.Equal("4", correctQ1.AnswerText);
        // Q2: correct should be "Paris"
        var correctQ2 = questions[1].Answers.Single(a => a.IsCorrect);
        Assert.Equal("Paris", correctQ2.AnswerText);
    }

    [Fact]
    public void Import_NoAntwoordenSection_ThrowsImportException()
    {
        var path = WriteTempTxt("1. Q?\nA. Yes\nB. No\n");
        Assert.Throws<ImportException>(() => new TxtQuestionImporter().Import(path));
    }

    [Fact]
    public void Format_IsTxt()
        => Assert.Equal("txt", new TxtQuestionImporter().Format);
}
