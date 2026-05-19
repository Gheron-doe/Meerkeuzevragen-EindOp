using MV_BL.Domain;
using MV_BL.Exceptions;
using MV_BL.Interfaces;

namespace MV_BL.Services;

public class QuestionService
{
    private readonly IQuestionRepository _repo;
    private readonly ITopicRepository _topicRepo;

    public QuestionService(IQuestionRepository repo, ITopicRepository topicRepo)
    {
        _repo = repo;
        _topicRepo = topicRepo;
    }

    public IReadOnlyList<Question> GetAll() => _repo.GetAll(excludeFlagged: true);

    public IReadOnlyList<Question> GetByTopic(int topicId, int? difficulty = null, bool activeOnly = false)
    {
        if (_topicRepo.GetById(topicId) is null)
            throw new TopicNotFoundException(topicId);
        return _repo.GetByTopic(topicId, excludeFlagged: true, activeOnly: activeOnly, difficulty: difficulty);
    }

    public int AddQuestion(
        int topicId,
        string text,
        int difficulty,
        IEnumerable<(string text, bool isCorrect, string? feedback)> answers,
        string? feedback = null)
    {
        ValidateInputs(topicId, text, difficulty, answers, out var ansList);

        var q = new Question
        {
            TopicId = topicId,
            QuestionText = text.Trim(),
            DifficultyLevel = difficulty,
            IsFlagged = false,
            IsActive = true,
            Feedback = feedback,
            CreatedAt = DateTime.UtcNow,
            Answers = ansList.Select((a, i) => new Answer
            {
                AnswerText = a.text.Trim(),
                IsCorrect = a.isCorrect,
                Feedback = a.feedback,
                OriginalOrder = i
            }).ToList()
        };
        return _repo.Add(q);
    }


    public void UpdateQuestion(
        int questionId,
        string text,
        int difficulty,
        IEnumerable<(int id, string text, bool isCorrect, string? feedback)> answers,
        string? feedback = null)
    {
        var q = _repo.GetById(questionId)
            ?? throw new MeerkeuzevragenException($"Question {questionId} not found.");

        if (string.IsNullOrWhiteSpace(text))
            throw new QuestionValidationException("Question text is empty.");
        if (difficulty < InvalidDifficultyException.Min || difficulty > InvalidDifficultyException.Max)
            throw new InvalidDifficultyException(difficulty);

        var ansList = answers
            .Where(a => a.id > 0 || !string.IsNullOrWhiteSpace(a.text))
            .ToList();

        if (ansList.Count < 2)
            throw new QuestionValidationException("A question needs at least 2 answers.");
        if (ansList.Count(a => a.isCorrect) != 1)
            throw new QuestionValidationException("A question must have exactly one correct answer.");
        if (ansList.Any(a => string.IsNullOrWhiteSpace(a.text)))
            throw new QuestionValidationException("Answer text cannot be empty.");

        q.QuestionText = text.Trim();
        q.DifficultyLevel = difficulty;
        q.Feedback = feedback;
        q.Answers = ansList.Select((a, i) => new Answer
        {
            Id = a.id,
            QuestionId = questionId,
            AnswerText = a.text.Trim(),
            IsCorrect = a.isCorrect,
            Feedback = a.feedback,
            OriginalOrder = i
        }).ToList();

        _repo.UpdateWithAnswers(q);
    }

    public void Deactivate(int questionId) => _repo.Deactivate(questionId);

    public void Activate(int questionId) => _repo.Activate(questionId);

    public void Flag(int questionId) => _repo.Flag(questionId);


    private void ValidateInputs(
        int topicId,
        string text,
        int difficulty,
        IEnumerable<(string text, bool isCorrect, string? feedback)> answers,
        out List<(string text, bool isCorrect, string? feedback)> ansList)
    {
        if (_topicRepo.GetById(topicId) is null)
            throw new TopicNotFoundException(topicId);
        if (string.IsNullOrWhiteSpace(text))
            throw new QuestionValidationException("Question text is empty.");
        if (difficulty < InvalidDifficultyException.Min || difficulty > InvalidDifficultyException.Max)
            throw new InvalidDifficultyException(difficulty);

        ansList = answers.ToList();
        if (ansList.Count < 2)
            throw new QuestionValidationException("A question needs at least 2 answers.");
        if (ansList.Count(a => a.isCorrect) != 1)
            throw new QuestionValidationException("A question must have exactly one correct answer.");
        if (ansList.Any(a => string.IsNullOrWhiteSpace(a.text)))
            throw new QuestionValidationException("Answer text cannot be empty.");
    }
}
