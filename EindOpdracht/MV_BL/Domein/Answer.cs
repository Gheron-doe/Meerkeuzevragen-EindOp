namespace MV_BL.Domain;

public class Answer
{
	public int Id { get; set; }
	public int QuestionId { get; set; }
	public string AnswerText { get; set; } = string.Empty;
	public bool IsCorrect { get; set; }
	public int OriginalOrder { get; set; } // A=0, B=1, C=2, D=3
    public string? Feedback { get; set; }
}
