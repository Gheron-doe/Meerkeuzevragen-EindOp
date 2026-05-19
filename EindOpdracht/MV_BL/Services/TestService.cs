using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MV_BL.Services;

public class TestService
{
    private readonly ITestRepository _testRepo;
    private readonly IQuestionRepository _questionRepo;
    private readonly Random _random;

    public TestService(ITestRepository testRepo, IQuestionRepository questionRepo, Random? random = null)
    {
        _testRepo = testRepo;
        _questionRepo = questionRepo;
        _random = random ?? new Random();
    }

    public Test GenerateTest(
        int topicId,
        int questionCount,
        string title,
        int? difficulty = null,
        ScoringMode scoringStrategy = ScoringMode.SimplePercent)
    {
        var pool = _questionRepo.GetByTopic(topicId, activeOnly: true, difficulty: difficulty).ToList();
        if (pool.Count == 0)
            throw new NoQuestionsAvailableException(topicId, difficulty);
        if (pool.Count < questionCount)
            questionCount = pool.Count;

        var picked = pool.OrderBy(_ => _random.Next()).Take(questionCount).ToList();

        var test = new Test
        {
            TopicId = topicId,
            Title = title,
            CreatedAt = DateTime.UtcNow,
            Difficulty = difficulty,
            ScoringStrategy = scoringStrategy,
            IsFlagged = false
        };

        int order = 1;
        foreach (var q in picked)
        {
            var indices = Enumerable.Range(0, q.Answers.Count).OrderBy(_ => _random.Next()).ToList();
            test.Questions.Add(new TestQuestion
            {
                QuestionId = q.Id,
                QuestionOrder = order++,
                AnswerDisplayOrder = indices,
                Question = q
            });
        }

        var id = _testRepo.Add(test);
        test.Id = id;
        return test;
    }

    public Test GetById(int id)
        => _testRepo.GetById(id) ?? throw new TestNotFoundException(id);

    public IReadOnlyList<Test> GetAll() => _testRepo.GetAll();

    public void Deactivate(int testId) => _testRepo.Deactivate(testId);

    public void Export(int testId, string filePath, ITestExporter exporter, bool includeAnswers = false)
    {
        var test = GetById(testId);
        exporter.Export(test, filePath, includeAnswers);
    }
}
