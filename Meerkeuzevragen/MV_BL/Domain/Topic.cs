using MV_BL.Exceptions;

namespace MV_BL.Domain;

public class Topic
{
    public Topic(int id, string name, bool isFlagged)
    {
        Id = id;
        Name = name;
        IsFlagged = isFlagged;
    }

    public Topic(string name) { Name = name; }

    private int _id;
    public int Id
    {
        get => _id;
        set { if (value < 0) throw new TopicException("Topic Id cannot be negative."); _id = value; }
    }

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new TopicException("Topic name cannot be empty.");
            _name = value.Trim();
        }
    }
    public bool IsFlagged { get; set; } = false;
}
