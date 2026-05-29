using MV_BL.Domain;

namespace MV_WPF;

public class TestGridItem
{
    public TestGridItem(Test test)
    {
        Id                = test.Id;
        Title             = test.Title;
        CreatedAt         = test.CreatedAt;
        TopicsString      = test.TopicsString();
        DifficultiesString = test.DifficultiesString();

        SourceTest = test;
    }

    public int      Id                 { get; }
    public string   Title              { get; }
    public DateTime CreatedAt          { get; }
    public string   TopicsString       { get; }
    public string   DifficultiesString { get; }
    public Test     SourceTest         { get; }
}
