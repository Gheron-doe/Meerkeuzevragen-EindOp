using MV_BL.Exceptions;

namespace MV_BL.Domain;

public class TestAttempt
{
    public TestAttempt(int id, int testId, int userId, DateTime startedAt,
        DateTime? completedAt = null, string? feedback = null, List<AttemptAnswer>? answers = null)
    {
        Id = id;
        TestId = testId;
        UserId = userId;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Feedback = feedback;
        Answers = answers ?? new List<AttemptAnswer>();
    }
    public TestAttempt(int testId, int userId)
    {
        TestId = testId;
        UserId = userId;
        StartedAt = DateTime.Now;
    }

    private int _id;
    public int Id
    {
        get => _id;
        set { if (value < 0) throw new TestException("Attempt Id cannot be negative."); 
            _id = value; }
    }
    private int _testId;
    public int TestId
    {
        get => _testId;
        set { if (value < 0) throw new TestException("TestAttempt.TestId cannot be negative.");
            _testId = value; }
    }
    private int _userId;
    public int UserId
    {
        get => _userId;
        set { if (value < 0) throw new UserException("TestAttempt.UserId cannot be negative."); 
            _userId = value; }
    }

    public DateTime StartedAt { get; set; } = DateTime.Now;

    public DateTime? CompletedAt { get; set; }

    public string? Feedback { get; set; }

    public List<AttemptAnswer> Answers { get; set; } = new();

    public void Validate()
    {
        if (TestId <= 0)
            throw new TestException("TestAttempt.TestId must be a positive integer.");
        if (UserId <= 0)
            throw new UserException("TestAttempt.UserId must be a positive integer.");
    }

    public int CountCorrect(Test test)
    {
        int correct = 0;
        foreach (var aa in Answers)
        {
            var tq = test.Questions.FirstOrDefault(t => t.Id == aa.TestQuestionId);
            if (tq?.Question is null) continue;
            var correctAnswer = tq.Question.Answers.FirstOrDefault(a => a.IsCorrect);
            if (correctAnswer is null) continue;
            if (aa.SelectedAnswerId.HasValue && aa.SelectedAnswerId.Value == correctAnswer.Id)
                correct++;
        }
        return correct;
    }

    public double PercentScore(Test test)
    {
        int total = test.Questions.Count;
        if (total == 0) return 0;
        return Math.Round(100.0 * CountCorrect(test) / total, 1);
    }

    public string ToLetterString(Test test)
    {
        var map = Answers.ToDictionary(a => a.TestQuestionId);
        var chars = new List<char>();
        foreach (var tq in test.Questions.OrderBy(q => q.SortOrder))
        {
            char letter = '?';
            if (map.TryGetValue(tq.Id, out var aa) && aa.SelectedAnswerId.HasValue)
            {
                string s = tq.AnswerIdToLetter(aa.SelectedAnswerId.Value);
                if (s.Length == 1) letter = s[0];
            }
            chars.Add(letter);
        }
        return new string(chars.ToArray());
    }

    public static List<AttemptAnswer> AnswersFromLetters(int attemptId, Test test, string letters)
    {
        var list = new List<AttemptAnswer>();
        var ordered = test.Questions.OrderBy(tq => tq.SortOrder).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            int? answerId = null;
            if (i < letters.Length && letters[i] != '?')
                answerId = ordered[i].LetterToAnswerId(letters[i]);
            list.Add(new AttemptAnswer(attemptId, ordered[i].Id, answerId));
        }
        return list;
    }
}
