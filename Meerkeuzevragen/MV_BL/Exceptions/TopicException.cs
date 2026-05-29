namespace MV_BL.Exceptions;

public class TopicException : Exception
{
    public TopicException() { }
    public TopicException(string? message) : base(message) { }
    public TopicException(string? message, Exception? inner) : base(message, inner) { }
}
