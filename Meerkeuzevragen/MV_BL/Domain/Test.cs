using MV_BL.Exceptions;

namespace MV_BL.Domain;

public class Test
{
    public Test(int id, string title, DateTime createdAt, bool isFlagged,
        List<TestQuestion> questions, List<Topic> topics)
    {
        Id = id;
        Title = title;
        CreatedAt = createdAt;
        Questions = questions;
        Topics = topics;
        IsFlagged = isFlagged;
    }

    public Test(string title, List<TestQuestion> questions, List<Topic> topics)
    {
        Title = title;
        CreatedAt = DateTime.Now;
        Questions = questions;
        Topics = topics;
        IsFlagged = false;
    }

    private int _id;

    public int Id
    {
        get => _id;
        set { if (value < 0) throw new TestException("Test Id cannot be negative."); _id = value; }
    }

    private string _title = string.Empty;

    public string Title
    {
        get => _title;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new TestException("Test title cannot be empty.");
            _title = value.Trim();
        }
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsFlagged { get; set; } = false;

    public List<Topic> Topics { get; set; } = new();

    private List<TestQuestion> _questions = new();
    public List<TestQuestion> Questions
    {
        get => _questions;
        set => _questions = value;
    }

    public void Validate()
    {
        if (Questions.Count == 0)
            throw new TestException("Test must have at least one question.");
    }
    public static Test Generate(string title, IEnumerable<Question> pool,
        int totalCount, int countDiff1, int countDiff2, int countDiff3)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new TestException("Test title is required.");

        var poolList = pool.Where(q => !q.IsFlagged && q.IsActive)
            .DistinctBy(q => q.Id).ToList();

        var rng = new Random();
        var picked = new List<Question>();

        bool perDifficulty = countDiff1 > 0 || countDiff2 > 0 || countDiff3 > 0;
        if (perDifficulty)
        {
            picked.AddRange(poolList.Where(q => q.DifficultyLevel == 1).OrderBy(_ => rng.Next()).Take(countDiff1));
            picked.AddRange(poolList.Where(q => q.DifficultyLevel == 2).OrderBy(_ => rng.Next()).Take(countDiff2));
            picked.AddRange(poolList.Where(q => q.DifficultyLevel == 3).OrderBy(_ => rng.Next()).Take(countDiff3));
        }
        else
        {
            if (totalCount <= 0)
                throw new TestException("Question count must be > 0.");
            picked.AddRange(poolList.OrderBy(_ => rng.Next()).Take(totalCount));
        }

        if (picked.Count == 0)
            throw new TestException("No questions available for the requested topic(s)/difficulty.");

        picked = picked.OrderBy(_ => rng.Next()).ToList();

        var tqs = new List<TestQuestion>();
        for (int i = 0; i < picked.Count; i++)
            tqs.Add(new TestQuestion(picked[i].Id, i + 1, picked[i]));

        var topics = picked.SelectMany(q => q.Topics).DistinctBy(t => t.Id).ToList();
        return new Test(title, tqs, topics);
    }

    public string DifficultiesString()
        => string.Join(",", Questions
            .Where(tq => tq.Question is not null)
            .Select(tq => tq.Question!.DifficultyLevel)
            .Distinct()
            .OrderBy(x => x));


    public string TopicsString()
        => string.Join(", ", Topics.Select(t => t.Name));
}
