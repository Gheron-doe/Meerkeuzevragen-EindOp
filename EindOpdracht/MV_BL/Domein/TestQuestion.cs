namespace MV_BL.Domain;

public class TestQuestion
{
	public int Id { get; set; }
	public int TestId { get; set; }
	public int QuestionId { get; set; }
	public int QuestionOrder { get; set; }
	public List<int> AnswerDisplayOrder { get; set; } = new();
	public Question? Question { get; set; }
}
