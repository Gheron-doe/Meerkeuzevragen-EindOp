using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;
using MV_Util.Factories;
using MV_Util.Registries;

namespace MV_xUnitTest;


public class UnitTestFactory
{
	// ExporterFactory 

	[Fact]
	public void Test_ExporterFactory_DefaultFormat_Valid()
	{
		var f = new ExporterFactory();
		Assert.Contains("txt", f.AvailableFormats);
		Assert.IsType<TxtTestExporter>(f.Create("txt"));
	}

	[Theory]
	[InlineData("pdf")]
	[InlineData("json")]
	[InlineData("")]
	public void Test_ExporterFactory_UnknownFormat_Invalid(string format)
	{
		var f = new ExporterFactory();
		Assert.Throws<UnsupportedFormatException>(() => f.Create(format));
	}

	[Fact]
	public void Test_ExporterFactory_CustomRegister_Valid()
	{
		var f = new ExporterFactory();
		f.Register(new DummyExporter());

		Assert.Contains("dummy", f.AvailableFormats);
		Assert.IsType<DummyExporter>(f.Create("dummy"));
	}

	// ImporterFactory 

	[Fact]
	public void Test_ImporterFactory_DefaultFormat_Valid()
	{
		var f = new ImporterFactory();
		Assert.Contains("txt", f.AvailableFormats);
	}

	[Theory]
	[InlineData("json")]
	[InlineData("csv")]
	[InlineData("")]
	public void Test_ImporterFactory_UnknownFormat_Invalid(string format)
	{
		var f = new ImporterFactory();
		Assert.Throws<UnsupportedFormatException>(() => f.Create(format));
	}

	// BulkInputParserFactory

	[Fact]
	public void Test_BulkInputParserFactory_DefaultFormat_Valid()
	{
		var f = new BulkInputParserFactory();
		Assert.Contains("csv", f.AvailableFormats);
		Assert.IsType<CsvBulkInputParser>(f.Create("csv"));
	}

	[Theory]
	[InlineData("txt")]
	[InlineData("json")]
	[InlineData("")]
	public void Test_BulkInputParserFactory_UnknownFormat_Invalid(string format)
	{
		var f = new BulkInputParserFactory();
		Assert.Throws<UnsupportedFormatException>(() => f.Create(format));
	}

	// ScoringStrategyFactory

	[Fact]
	public void Test_ScoringStrategyFactory_DefaultMode_Valid()
	{
		var f = new ScoringStrategyFactory();
		Assert.Contains(ScoringMode.SimplePercent, f.AvailableModes);
		Assert.IsType<SimplePercentScoring>(f.Create(ScoringMode.SimplePercent));
	}

	//[Fact]
	//public void Test_ScoringStrategyFactory_UnknownMode_Invalid()
	//{
	//	var f = new ScoringStrategyFactory();
	//	Assert.Throws<UnsupportedFormatException>(() =>
	//		f.Create(ScoringMode.NegativeMarking));
	//}

	// Stub 

	private class DummyExporter : ITestExporter
	{
		public string Format => "dummy";
		public void Export(Test test, string filePath, bool includeAnswers = false) { }
	}
}
