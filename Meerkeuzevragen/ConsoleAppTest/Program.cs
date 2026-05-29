using Microsoft.Extensions.Configuration;
using MV_BL.Domain;
using MV_BL.Services;
using MV_Util.Factories;

namespace ConsoleAppTest;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("ConsoleAppTest");

        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        var config = builder.Build();
        var connectionString = config.GetConnectionString("SQLServerConnection");
        var databaseType = config.GetSection("AppSettings")["databaseType"]; 
        var repos = RepositoryFactory.Create(connectionString, databaseType);

        var topicRepo    = repos.CreateTopicRepository();
        var questionRepo = repos.CreateQuestionRepository();
        var testRepo     = repos.CreateTestRepository(questionRepo);
        var userRepo     = repos.CreateUserRepository();
        var attemptRepo  = repos.CreateAttemptRepository();

        var topicService    = new TopicService(topicRepo);
        var questionService = new QuestionService(questionRepo);
        var testService     = new TestService(testRepo);
        var importService   = new ImportService(questionRepo);
        var attemptService  = new AttemptService(attemptRepo);
        var userService     = new UserService(userRepo);

        Console.WriteLine($"Topics:   {topicService.GetAll().Count}");
        Console.WriteLine($"Questions:{questionService.GetAll().Count}");
        Console.WriteLine($"Tests:    {testService.GetAll().Count}");
        Console.WriteLine($"Users:    {userService.GetAll().Count}");
        Console.WriteLine($"Attempts: {attemptService.GetAll().Count}");
        Console.WriteLine("Done.");
    }
}
