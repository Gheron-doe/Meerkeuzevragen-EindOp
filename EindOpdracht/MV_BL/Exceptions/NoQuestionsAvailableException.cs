using MV_BL.Exceptions;

namespace MV_BL.Exceptions;

public class NoQuestionsAvailableException : MeerkeuzevragenException
{
	public int TopicId { get; }
	public int? Difficulty { get; }
	public NoQuestionsAvailableException(int topicId, int? difficulty)
		: base($"No questions available for topic {topicId}" + (difficulty.HasValue ? $" at difficulty {difficulty}." : "."))
	{
		TopicId = topicId;
		Difficulty = difficulty;
	}
}
