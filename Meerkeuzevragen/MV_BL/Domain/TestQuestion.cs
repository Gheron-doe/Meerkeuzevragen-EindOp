using MV_BL.Exceptions;

namespace MV_BL.Domain;

public class TestQuestion
{
    public TestQuestion(int id, int testId, int questionId, int sortOrder, Question? question = null)
    {
        Id = id;
        TestId = testId;
        QuestionId = questionId;
        SortOrder = sortOrder;
        Question = question;
    }

    public TestQuestion(int questionId, int sortOrder, Question? question = null)
    {
        QuestionId = questionId;
        SortOrder = sortOrder;
        Question = question;
    }

    public int Id { get; set; }

    public int TestId { get; set; }

    public int QuestionId { get; set; }

    public int SortOrder { get; set; }

    public Question? Question { get; set; }

    public List<Answer> GetShuffledAnswers()
    {
        if (Question is null) throw new TestException("TestQuestion has no Question loaded.");
        int seed = SortOrder * 397 ^ QuestionId;
        var rng = new Random(seed);
        return Question.Answers.OrderBy(_ => rng.Next()).ToList();
    }

    public string AnswerIdToLetter(int answerId)
    {
        var shuffled = GetShuffledAnswers();
        int slot = shuffled.FindIndex(a => a.Id == answerId);
        return slot < 0 ? "?" : ((char)('A' + slot)).ToString();
    }

    public int? LetterToAnswerId(char letter)
    {
        var shuffled = GetShuffledAnswers();
        int slot = char.ToUpperInvariant(letter) - 'A';
        if (slot < 0 || slot >= shuffled.Count) return null;
        return shuffled[slot].Id;
    }
}
