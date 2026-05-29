using MV_BL.Exceptions;

namespace MV_BL.Domain;

public class Answer
{
    public Answer(int id, int questionId, string answerText, bool isCorrect, string? feedback = null)
    {
        Id = id;
        QuestionId = questionId;
        AnswerText = answerText;
        IsCorrect = isCorrect;
        Feedback = feedback;
    }

    public Answer(string answerText, bool isCorrect, string? feedback = null)
    {
        AnswerText = answerText;
        IsCorrect = isCorrect;
        Feedback = feedback;
    }

    private int _id;

    public int Id
    {
        get => _id;
        set { if (value < 0) throw new AnswerException("Answer Id cannot be negative."); _id = value; }
    }

    public int QuestionId { get; set; }

    private string _answerText = string.Empty;

    public string AnswerText
    {
        get => _answerText;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new AnswerException("Answer text cannot be empty.");
            _answerText = value.Trim();
        }
    }

    public bool IsCorrect { get; set; }

    public string? Feedback { get; set; }
}
