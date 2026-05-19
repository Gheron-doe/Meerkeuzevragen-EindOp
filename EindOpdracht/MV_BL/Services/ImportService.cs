using MV_BL.Exceptions;
using MV_BL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MV_BL.Services;

public class ImportService
{
    private readonly IQuestionRepository _questionRepo;
    private readonly ITopicRepository _topicRepo;

    public ImportService(IQuestionRepository questionRepo, ITopicRepository topicRepo)
    {
        _questionRepo = questionRepo;
        _topicRepo = topicRepo;
    }

    public int ParseAndSeed(string filePath, int topicId, IQuestionImporter importer, int difficulty = 1)
    {
        if (_topicRepo.GetById(topicId) is null)
            throw new TopicNotFoundException(topicId);
        if (!importer.CanImport(filePath))
            throw new MeerkeuzevragenException($"Importer '{importer.Format}' cannot read file '{filePath}'.");
        if (difficulty < InvalidDifficultyException.Min || difficulty > InvalidDifficultyException.Max)
            throw new InvalidDifficultyException(difficulty);

        var questions = importer.Import(filePath, topicId);
        int added = 0;
        foreach (var q in questions)
        {
            if (q.Answers.Count(a => a.IsCorrect) != 1)
                throw new AnswerKeyMismatchException($"Question '{q.QuestionText}' does not have exactly one correct answer.");
            q.DifficultyLevel = difficulty;
            q.IsFlagged = false;
            q.IsActive = true;
            _questionRepo.Add(q);
            added++;
        }
        return added;
    }
}
