using MV_BL.Exceptions;

namespace MV_BL.Exceptions;

public class InvalidDifficultyException : MeerkeuzevragenException
{
	public const int Min = 1;
	public const int Max = 3;
	public int Actual { get; }
	public InvalidDifficultyException(int actual)
		: base($"Difficulty must be between {Min} and {Max}, got {actual}.")
	{
		Actual = actual;
	}
}
