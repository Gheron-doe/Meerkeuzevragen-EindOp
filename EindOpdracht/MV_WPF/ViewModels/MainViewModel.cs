using MV_BL.Services;
using MV_Util.Factories;

namespace MV_WPF.ViewModels;

public class MainViewModel : ViewModelBase
{
	public TestManagerViewModel    TestManager    { get; }
	public QuestionEditorViewModel QuestionEditor { get; }
	public GradingViewModel        Grading        { get; }
	public ImportSourceViewModel   ImportSource   { get; }

	public MainViewModel(
		TopicService topicService,
		QuestionService questionService,
		TestService testService,
		ImportService importService,
		AttemptService attemptService,
		UserService userService,
		ExporterFactory exporterFactory,
		ImporterFactory importerFactory,
		BulkInputParserFactory bulkFactory,
		ScoringStrategyFactory scoringFactory)
	{
		TestManager    = new TestManagerViewModel(topicService, testService, attemptService, exporterFactory, scoringFactory);
		QuestionEditor = new QuestionEditorViewModel(topicService, questionService);
		Grading        = new GradingViewModel(testService, attemptService, userService, topicService, scoringFactory);
		ImportSource   = new ImportSourceViewModel(topicService, importService, importerFactory, attemptService, bulkFactory, testService);

		ImportSource.DataChanged += () =>
		{
			QuestionEditor.RefreshTopics();
			TestManager.Refresh();
			Grading.Refresh();
		};

		
		TestManager.DataChanged += () => Grading.Refresh();

	}
}
