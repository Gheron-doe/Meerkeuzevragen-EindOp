using MV_Util.Implementations;

namespace MV_xUnitTesting.UtilTests;

public class CsvBulkInputParserTests
{
    private static string WriteTempCsv(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".csv");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Parse_ValidRows_ReturnsCorrectBulkRows()
    {
        string csv = "1,ABCD\n2,BCDA,Good job\n";
        string path = WriteTempCsv(csv);
        var parser = new CsvBulkInputParser();
        var rows = parser.Parse(path).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(1,      rows[0].UserId);
        Assert.Equal("ABCD", rows[0].Answers);
        Assert.Null(rows[0].Feedback);

        Assert.Equal(2,          rows[1].UserId);
        Assert.Equal("BCDA",     rows[1].Answers);
        Assert.Equal("Good job", rows[1].Feedback);
    }

    [Fact]
    public void Parse_HeaderRow_IsSkipped()
    {
        string csv = "ID,Answers,Feedback\n1,ABCD\n";
        string path = WriteTempCsv(csv);
        var rows = new CsvBulkInputParser().Parse(path).ToList();
        Assert.Single(rows);
        Assert.Equal(1, rows[0].UserId);
    }

    [Fact]
    public void Parse_BlankLines_AreSkipped()
    {
        string csv = "\n1,ABCD\n\n2,BCDA\n\n";
        string path = WriteTempCsv(csv);
        var rows = new CsvBulkInputParser().Parse(path).ToList();
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void Parse_NonIntegerUserId_RowSkipped()
    {
        string csv = "alice,ABCD\n2,BCDA\n";
        string path = WriteTempCsv(csv);
        var rows = new CsvBulkInputParser().Parse(path).ToList();
        Assert.Single(rows);
        Assert.Equal(2, rows[0].UserId);
    }

    [Fact]
    public void Parse_AnswersNormalisedToUppercase()
    {
        string csv = "1,abcd\n";
        string path = WriteTempCsv(csv);
        var rows = new CsvBulkInputParser().Parse(path).ToList();
        Assert.Equal("ABCD", rows[0].Answers);
    }

    [Fact]
    public void Parse_EmptyFeedback_StoredAsNull()
    {
        string csv = "1,ABCD,\n";
        string path = WriteTempCsv(csv);
        var rows = new CsvBulkInputParser().Parse(path).ToList();
        Assert.Null(rows[0].Feedback);
    }

    [Fact]
    public void Format_IsCsv()
        => Assert.Equal("csv", new CsvBulkInputParser().Format);
}
