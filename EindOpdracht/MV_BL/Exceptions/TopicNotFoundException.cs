namespace MV_BL.Exceptions;

public class TopicNotFoundException : MeerkeuzevragenException
{
	public int TopicId { get; }
	public TopicNotFoundException(int topicId)
		: base($"Topic with id {topicId} was not found.")
	{
		TopicId = topicId;
	}
}
