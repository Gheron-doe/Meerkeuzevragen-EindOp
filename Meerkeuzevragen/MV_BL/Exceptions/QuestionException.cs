namespace MV_BL.Exceptions;

public class QuestionException : Exception
{
    public QuestionException() { }
    public QuestionException(string? message) : base(message) { }
    public QuestionException(string? message, Exception? inner) : base(message, inner) { }
}
