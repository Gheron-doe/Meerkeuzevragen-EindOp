namespace MV_BL.Exceptions;

public class AnswerException : Exception
{
    public AnswerException() { }
    public AnswerException(string? message) : base(message) { }
    public AnswerException(string? message, Exception? inner) : base(message, inner) { }
}
