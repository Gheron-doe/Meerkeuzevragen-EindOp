using MV_BL.Exceptions;
using MV_Util.Factories;

namespace MV_xUnitTesting.FactoryTests;

public class ImporterFactoryTests
{
    [Fact]
    public void Create_Txt_ReturnsNonNull()
    {
        var imp = ImporterFactory.Create("txt");
        Assert.NotNull(imp);
        Assert.Equal("txt", imp.Format);
    }

    [Fact]
    public void Create_TxtCaseInsensitive_Works()
    {
        var imp = ImporterFactory.Create("TXT");
        Assert.Equal("txt", imp.Format);
    }

    [Fact]
    public void Create_UnknownFormat_ThrowsImportException()
        => Assert.Throws<ImportException>(() => ImporterFactory.Create("pdf"));

    [Fact]
    public void AvailableFormats_ContainsTxt()
        => Assert.Contains("txt", ImporterFactory.AvailableFormats);

}

public class ExporterFactoryTests
{
    [Fact]
    public void Create_Txt_ReturnsNonNull()
    {
        var exp = ExporterFactory.Create("txt");
        Assert.NotNull(exp);
        Assert.Equal("txt", exp.Format);
    }

    [Fact]
    public void Create_UnknownFormat_ThrowsImportException()
        => Assert.Throws<ImportException>(() => ExporterFactory.Create("docx"));

    [Fact]
    public void AvailableFormats_ContainsTxt()
        => Assert.Contains("txt", ExporterFactory.AvailableFormats);
}

public class BulkInputParserFactoryTests
{
    [Fact]
    public void Create_Csv_ReturnsNonNull()
    {
        var p = BulkInputParserFactory.Create("csv");
        Assert.NotNull(p);
        Assert.Equal("csv", p.Format);
    }

    [Fact]
    public void Create_CsvCaseInsensitive_Works()
    {
        var p = BulkInputParserFactory.Create("CSV");
        Assert.Equal("csv", p.Format);
    }

    [Fact]
    public void Create_UnknownFormat_ThrowsImportException()
        => Assert.Throws<ImportException>(() => BulkInputParserFactory.Create("json"));

    [Fact]
    public void AvailableFormats_ContainsCsv()
        => Assert.Contains("csv", BulkInputParserFactory.AvailableFormats);
}

public class RepositoryFactoryTests
{
    [Fact]
    public void Create_Sql_ReturnsNonNull()
    {
        var f = RepositoryFactory.Create("SQL", "Server=localhost;Database=test;");
        Assert.NotNull(f);
    }

    [Fact]
    public void Create_CaseInsensitive_Works()
    {
        var f = RepositoryFactory.Create("sql", "Server=x;");
        Assert.NotNull(f);
    }

    [Fact]
    public void Create_UnknownType_ThrowsInvalidOperationException()
        => Assert.Throws<InvalidOperationException>(
            () => RepositoryFactory.Create("mongo", "conn"));
}
