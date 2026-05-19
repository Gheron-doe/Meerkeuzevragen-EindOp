namespace MV_BL.Exceptions;

public class MeerkeuzevragenException : Exception
{
	public MeerkeuzevragenException() { }
	public MeerkeuzevragenException(string message) : base(message) { }
	public MeerkeuzevragenException(string message, Exception inner) : base(message, inner) { }
}
