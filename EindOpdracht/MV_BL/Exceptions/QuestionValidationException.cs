namespace MV_BL.Exceptions;

public class QuestionValidationException : MeerkeuzevragenException
{
	public string Reason { get; }
	public QuestionValidationException(string reason)
		: base($"Question is invalid: {reason}")
	{
		Reason = reason;
	}
}
