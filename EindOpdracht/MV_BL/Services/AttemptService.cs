using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;

namespace MV_BL.Services;

public class BulkImportResult
{
    public int UserId { get; init; }
    public int CorrectCount { get; init; }
    public int Total { get; init; }
    public List<QuestionFeedback> Feedback { get; init; } = new();
    public bool Persisted { get; init; }
}
public class AttemptService
{
    private readonly IAttemptRepository _attemptRepo;
    private readonly ITestRepository _testRepo;
    private readonly IUserRepository _userRepo;

    public AttemptService(IAttemptRepository attemptRepo, ITestRepository testRepo, IUserRepository userRepo)
    {
        _attemptRepo = attemptRepo;
        _testRepo = testRepo;
        _userRepo = userRepo;
    }

    public (int attemptId, DateTime startedAt) StartAttempt(int testId, string username)
    {
        _ = _testRepo.GetById(testId) ?? throw new TestNotFoundException(testId);

        var user = _userRepo.GetByUsername(username);
        if (user is null)
        {
            var newId = _userRepo.Add(new User { Username = username });
            user = _userRepo.GetById(newId)!;
        }

        var startedAt = DateTime.UtcNow;
        var attempt = new TestAttempt
        {
            TestId = testId,
            UserId = user.Id,
            StartedAt = startedAt
        };
        int attemptId = _attemptRepo.StartAttempt(attempt);
        return (attemptId, startedAt);
    }
    public (TestAttempt attempt, GradingResult grading) CompleteAttempt(
        int attemptId,
        int testId,
        IReadOnlyList<(int testQuestionId, int? selectedAnswerId)> selections)
    {
        var test = _testRepo.GetById(testId) ?? throw new TestNotFoundException(testId);
        var grading = ScoreService.Grade(test, selections);
        var completedAt = DateTime.UtcNow;

        _attemptRepo.Complete(attemptId, completedAt, grading.CorrectCount, grading.Answers);

        var attempt = _attemptRepo.GetById(attemptId)!;
        return (attempt, grading);
    }
    public void SetFeedback(int attemptId, string? feedback)
        => _attemptRepo.UpdateFeedback(attemptId, feedback);

    public IReadOnlyList<TestAttempt> GetAll() => _attemptRepo.GetAll();
    public IReadOnlyList<TestAttempt> GetByTest(int testId) => _attemptRepo.GetByTest(testId);
    public TestAttempt? GetById(int id) => _attemptRepo.GetById(id);

    public IReadOnlyList<BulkImportResult> ImportFromFile(int testId, string filePath, IBulkInputParser parser)
    {
        var test = _testRepo.GetById(testId) ?? throw new TestNotFoundException(testId);
        var results = new List<BulkImportResult>();

        foreach (var row in parser.Parse(filePath))
        {
            var grading = ScoreService.GradeFromLetters(test, row.Answers);
            var user = _userRepo.GetById(row.UserId);
            bool persisted = false;

            if (user is not null)
            {
                var startedAt = DateTime.UtcNow;
                var attempt = new TestAttempt { TestId = testId, UserId = user.Id, StartedAt = startedAt };
                int attemptId = _attemptRepo.StartAttempt(attempt);
                _attemptRepo.Complete(attemptId, startedAt, grading.CorrectCount, grading.Answers);
                if (!string.IsNullOrWhiteSpace(row.Feedback))
                    _attemptRepo.UpdateFeedback(attemptId, row.Feedback);
                persisted = true;
            }

            results.Add(new BulkImportResult
            {
                UserId = row.UserId,
                CorrectCount = grading.CorrectCount,
                Total = grading.Total,
                Feedback = grading.Feedback,
                Persisted = persisted
            });
        }
        return results;
    }
    public void ExportAttemptsToCsv(int testId, string filePath)
    {
        var test = _testRepo.GetById(testId) ?? throw new TestNotFoundException(testId);
        var attempts = _attemptRepo.GetByTest(testId)
            .Where(a => a.CompletedAt.HasValue)
            .ToList();

        using var writer = new StreamWriter(filePath);
        writer.WriteLine("IDGebruiker,Antwoorden,Feedback");
        foreach (var attempt in attempts)
        {
            var full = _attemptRepo.GetById(attempt.Id);
            if (full is null) continue;
            string letters = ScoreService.ToLetterString(test, full.Answers);
            string feedback = attempt.Feedback ?? string.Empty;
            writer.WriteLine($"{attempt.UserId},{letters},{feedback}");
        }
    }
}
