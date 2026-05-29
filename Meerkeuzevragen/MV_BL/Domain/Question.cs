using MV_BL.Exceptions;

namespace MV_BL.Domain;

public class Question
{
    public Question(int id, string questionText, int difficultyLevel, bool isFlagged = false,
        DateTime? createdAt = null, string? feedback = null,
        List<Answer>? answers = null, List<Topic>? topics = null)
    {
        Id = id;
        QuestionText = questionText;
        DifficultyLevel = difficultyLevel;
        IsFlagged = isFlagged;
        CreatedAt = createdAt ?? DateTime.Now;
        Feedback = feedback;
        Answers = answers ?? new List<Answer>();
        Topics = topics ?? new List<Topic>();
    }

    public Question(string questionText, int difficultyLevel, string? feedback = null)
    {
        QuestionText = questionText;
        DifficultyLevel = difficultyLevel;
        Feedback = feedback;
        CreatedAt = DateTime.Now;
    }

    private int _id;

    public int Id
    {
        get => _id;
        set { if (value < 0) throw new QuestionException("Question Id cannot be negative."); _id = value; }
    }

    private string _questionText = string.Empty;

    public string QuestionText
    {
        get => _questionText;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new QuestionException("Question text cannot be empty.");
            _questionText = value.Trim();
        }
    }

    private int _difficultyLevel = 1;

    public int DifficultyLevel
    {
        get => _difficultyLevel;
        set
        {
            if (value < 1 || value > 3)
                throw new QuestionException($"Difficulty must be 1-3, got {value}.");
            _difficultyLevel = value;
        }
    }

    public bool IsFlagged { get; set; } = false;

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public string? Feedback { get; set; }

    private List<Answer> _answers = new();

    public List<Answer> Answers
    {
        get => _answers;
        set
        {
            if (value.Count < 2)
                throw new QuestionException("A question needs at least 2 answers.");
            int correctCount = value.Count(a => a.IsCorrect);
            if (correctCount != 1)
                throw new AnswerException($"A question must have exactly one correct answer (got {correctCount}).");
            if (Id > 0)
                foreach (Answer a in value)
                    if (a.QuestionId != 0 && a.QuestionId != Id)
                        throw new AnswerException($"Answer #{a.Id} has QuestionId={a.QuestionId}, expected {Id}.");
            _answers = value;
        }
    }

    public List<Topic> Topics { get; set; } = new();
}
