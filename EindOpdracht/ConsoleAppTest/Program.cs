using MV_BL.Domain;
using MV_BL.Services;
using MV_Util.Factories;

namespace ConsoleAppTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
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

            // create topics
            var sqlTopicId = topicRepo.Add(new Topic { Name = "SQL Basics" });
            var netTopicId = topicRepo.Add(new Topic { Name = ".NET Fundamentals" });

            // add questions sql
            var q1 = questionService.AddQuestion(
                sqlTopicId,
                text: "Wat doet een SELECT statement?",
                difficulty: 1,
                answers: new[]
                {
                    ("Gegevens ophalen uit een tabel",       true,  (string?)"Correct! SELECT haalt rijen op."),
                    ("Gegevens invoegen in een tabel",        false, (string?)"Dat is INSERT, niet SELECT."),
                    ("Een tabel aanmaken",                    false, (string?)"Dat is CREATE TABLE."),
                    ("Een rij verwijderen",                   false, (string?)"Dat is DELETE.")
                },
                feedback: "Denk aan CRUD: Create/Read/Update/Delete. SELECT is de Read-operatie.");

            var q2 = questionService.AddQuestion(
                sqlTopicId,
                text: "Welke sleutelwoord filtert rijen in een query?",
                difficulty: 1,
                answers: new[]
                {
                    ("FROM",    false, (string?)null),
                    ("WHERE",   true,  (string?)"Correct! WHERE filtert op een conditie."),
                    ("GROUP BY",false, (string?)"GROUP BY groepeert, filtert niet."),
                    ("ORDER BY",false, (string?)"ORDER BY sorteert, filtert niet.")
                });

            var q3 = questionService.AddQuestion(
                sqlTopicId,
                text: "Wat is een PRIMARY KEY?",
                difficulty: 2,
                answers: new[]
                {
                    ("Een unieke identifier voor elke rij",          true,  (string?)"Juist! Elke rij heeft één unieke PK."),
                    ("Een kolom die null-waarden toestaat",           false, (string?)"PK's zijn nooit NULL."),
                    ("Een koppeling naar een andere tabel",           false, (string?)"Dat is een FOREIGN KEY."),
                    ("Een index op een niet-unieke kolom",            false, (string?)null)
                },
                feedback: "Primary key = uniek + not null per definitie.");

            var q4 = questionService.AddQuestion(
                sqlTopicId,
                text: "Welke JOIN geeft alle rijen van BEIDE tabellen?",
                difficulty: 3,
                answers: new[]
                {
                    ("INNER JOIN", false, (string?)"INNER JOIN geeft alleen overeenkomsten."),
                    ("LEFT JOIN",  false, (string?)"LEFT JOIN geeft alle links + overeenkomsten rechts."),
                    ("FULL JOIN",  true,  (string?)"Correct! FULL OUTER JOIN geeft alle rijen van beide kanten."),
                    ("CROSS JOIN", false, (string?)"CROSS JOIN geeft het Cartesisch product.")
                });

            var q5 = questionService.AddQuestion(
                sqlTopicId,
                text: "Welke aggregate-functie telt het aantal rijen?",
                difficulty: 1,
                answers: new[]
                {
                    ("SUM",   false, (string?)"SUM telt waarden op, niet rijen."),
                    ("AVG",   false, (string?)"AVG berekent het gemiddelde."),
                    ("COUNT", true,  (string?)"Correct! COUNT(*) telt alle rijen."),
                    ("MAX",   false, (string?)"MAX geeft de hoogste waarde.")
                });

            // add questions .net
            questionService.AddQuestion(
                netTopicId,
                text: "Wat is een interface in C#?",
                difficulty: 1,
                answers: new[] 
                {
                    ("Een contract zonder implementatie",              true,  (string?)"Correct! Interface = alleen handtekeningen."),
                    ("Een klasse met alleen private leden",            false, (string?)null),
                    ("Een static klasse",                              false, (string?)null),
                    ("Een abstract methode zonder parameters",         false, (string?)null)
                });

            questionService.AddQuestion(
                netTopicId,
                text: "Welk sleutelwoord maakt een methode overschrijfbaar?",
                difficulty: 2,
                answers: new[]
                {
                    ("static",   false, (string?)null),
                    ("sealed",   false, (string?)"sealed verhindert overschrijving."),
                    ("virtual",  true,  (string?)"Correct! virtual staat override toe in afgeleide klassen."),
                    ("abstract", false, (string?)"abstract dwingt overschrijving af maar heeft zelf geen body.")
                });

            // generate test
            var sqlTest = testService.GenerateTest(sqlTopicId, questionCount: 3, title: "SQL Quiz A");
            var fullTest = testService.GenerateTest(sqlTopicId, questionCount: 5, title: "SQL Volledig");

            // users and complete attempts
            // alice: alle antwoorden
            var (aliceAttemptId, aliceStart) = attemptService.StartAttempt(sqlTest.Id, username: "alice");

            // Bouw antwoorden
            var aliceAnswers = sqlTest.Questions
                .Select(tq =>
                {
                    int slot0OrigOrder = tq.AnswerDisplayOrder[0];
                    var ans = tq.Question!.Answers.First(a => a.OriginalOrder == slot0OrigOrder);
                    return (tq.Id, (int?)ans.Id);
                })
                .ToArray();

            var (aliceAttempt, aliceGrading) = attemptService.CompleteAttempt(aliceAttemptId, sqlTest.Id, aliceAnswers);
            attemptService.SetFeedback(aliceAttemptId, "Goed geprobeerd, Alice!");

            // Bob: antwoordt "slot A" op alles
            var (bobAttemptId, _) = attemptService.StartAttempt(fullTest.Id, username: "bob");

            var bobAnswers = fullTest.Questions
                .Select(tq =>
                {
                    int slot0OrigOrder = tq.AnswerDisplayOrder[0];
                    var ans = tq.Question!.Answers.First(a => a.OriginalOrder == slot0OrigOrder);
                    return (tq.Id, (int?)ans.Id);
                })
                .ToArray();

            var (_, bobGrading) = attemptService.CompleteAttempt(bobAttemptId, fullTest.Id, bobAnswers);

            // wrong-answer feedback
            var wrongItems = aliceGrading.Feedback.Where(f => !f.IsCorrect).ToList();

            // deactivate questiın + regenerate test
            questionService.Deactivate(q4); // FULL JOIN-vraag deactiveren
            var activeQ = questionRepo.GetByTopic(sqlTopicId, activeOnly: true);

            var refreshedTest = testService.GenerateTest(sqlTopicId, questionCount: 3, title: "SQL Quiz B");
            Console.WriteLine("Done!");
        }
    }
}
