namespace MV_BL.Domain;

public class AttemptAnswer
{
	public int Id { get; set; }
	public int AttemptId { get; set; }
	public int TestQuestionId { get; set; }
    public int? SelectedAnswerId { get; set; }
    public bool IsCorrect { get; set; }
}
