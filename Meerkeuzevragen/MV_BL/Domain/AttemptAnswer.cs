namespace MV_BL.Domain;

public class AttemptAnswer
{
    public AttemptAnswer(int attemptId, int testQuestionId, int? selectedAnswerId)
    {
        AttemptId = attemptId;
        TestQuestionId = testQuestionId;
        SelectedAnswerId = selectedAnswerId;
    }

    public AttemptAnswer(int testQuestionId, int? selectedAnswerId)
    {
        TestQuestionId = testQuestionId;
        SelectedAnswerId = selectedAnswerId;
    }

    public int AttemptId { get; set; }

    public int TestQuestionId { get; set; }

    public int? SelectedAnswerId { get; set; }
}
