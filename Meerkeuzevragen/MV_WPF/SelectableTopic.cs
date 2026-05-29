using MV_BL.Domain;

namespace MV_WPF;

public class SelectableTopic
{
    public SelectableTopic(Topic topic)
    {
        Topic = topic;
    }

    public Topic Topic { get; }
    public int    Id   => Topic.Id;
    public string Name => Topic.Name;
    public bool IsSelected { get; set; }
}
