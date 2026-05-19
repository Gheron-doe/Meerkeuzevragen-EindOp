using MV_BL.Exceptions;

namespace MV_BL.Exceptions;

public class AnswerKeyMismatchException : MeerkeuzevragenException
{
	public string Reason { get; }
	public AnswerKeyMismatchException(string reason)
		: base($"Answer key does not match questions: {reason}")
	{
		Reason = reason;
	}
}
