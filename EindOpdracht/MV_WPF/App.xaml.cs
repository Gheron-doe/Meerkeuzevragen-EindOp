using MV_BL.Services;
using MV_Util.Factories;
using MV_WPF.ViewModels;
using System.IO;
using System.Windows;

namespace MV_WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static MainViewModel MainVM { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        //  Repositories
        var appsettings = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        var repoFactory = new RepositoryFactory();
        var repos = repoFactory.CreateFromSettings(appsettings);

        var topicRepo = repos.CreateTopicRepository();
        var questionRepo = repos.CreateQuestionRepository();
        var testRepo = repos.CreateTestRepository(questionRepo);
        var userRepo = repos.CreateUserRepository();
        var attemptRepo = repos.CreateAttemptRepository();

        //  Import / Export / Scoring 
        var exporterFactory = new ExporterFactory();
        var importerFactory = new ImporterFactory();
        var bulkFactory = new BulkInputParserFactory();
        var scoringFactory = new ScoringStrategyFactory();

        //  Services 
        var topicService = new TopicService(topicRepo);
        var questionService = new QuestionService(questionRepo, topicRepo);
        var testService = new TestService(testRepo, questionRepo);
        var importService = new ImportService(questionRepo, topicRepo);
        var attemptService = new AttemptService(attemptRepo, testRepo, userRepo);
        var userService = new UserService(userRepo);

        MainVM = new MainViewModel(
            topicService, questionService, testService, importService, attemptService, userService,
            exporterFactory, importerFactory, bulkFactory, scoringFactory);
    }
}
