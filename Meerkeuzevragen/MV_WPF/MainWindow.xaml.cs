using Microsoft.Extensions.Configuration;
using MV_BL.Services;
using MV_Util.Factories;
using System.IO;
using System.Windows;

namespace MV_WPF;

public partial class MainWindow : Window
{
    private string connectionString;
    private string databaseType;
    public MainWindow()
    {
        InitializeComponent();
        ReadConfig();
        var repositories = RepositoryFactory.Create(databaseType, connectionString);

        var topicRepo = repositories.CreateTopicRepository();
        var questionRepo = repositories.CreateQuestionRepository();
        var testRepo = repositories.CreateTestRepository(questionRepo);
        var userRepo = repositories.CreateUserRepository();
        var attemptRepo = repositories.CreateAttemptRepository();

        var topicService = new TopicService(topicRepo);
        var questionService = new QuestionService(questionRepo);
        var testService = new TestService(testRepo);
        var importService = new ImportService(questionRepo);
        var attemptService = new AttemptService(attemptRepo);
        var userService = new UserService(userRepo);

        TestsTab.Setup(topicService, questionService, testService, attemptService, userService);
        QuestionsTab.Setup(topicService, questionService);
        GradingTab.Setup(testService, attemptService, userService, topicService);
        ImportTab.Setup(topicService, importService, attemptService, userService, testService);

        ImportTab.DataChanged += () =>
        {
            QuestionsTab.RefreshTopics();
            TestsTab.Refresh();
            GradingTab.Refresh();
        };

        TestsTab.DataChanged += () =>
        {
            GradingTab.Refresh();
        };

        QuestionsTab.DataChanged += () =>
        {
            TestsTab.Refresh();
            GradingTab.Refresh();
        };
    }
    private void ReadConfig()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        var config = builder.Build();
        connectionString = config.GetConnectionString("SQLServerConnection");
        databaseType = config.GetSection("AppSettings")["databaseType"];
    }
}
