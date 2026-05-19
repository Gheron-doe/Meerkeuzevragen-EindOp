namespace MV_BL.Exceptions;

public class TestNotFoundException : MeerkeuzevragenException
{
	public int TestId { get; }
	public TestNotFoundException(int testId)
		: base($"Test with id {testId} was not found.")
	{
		TestId = testId;
	}
}
