namespace MV_WPF;

// Represents one question in the AttemptViewerWindow (grader's attempt detail).
// Bundles question metadata, correctness flags, feedback strings and the list of
// answer options so a single nested ItemsControl in XAML can render everything.
//
// Why bundle answers inside the question row?
//   AttemptViewerWindow shows each question and its options as one block.
//   A nested ItemsControl binds to Options for the answer list.
//   Alternative: keep two separate flat lists (questions + answers) and join them
//   in XAML via a converter — but that makes the binding logic much harder to follow.
public class AttemptQuestionRow
{
    public int Order { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public bool IsSkipped { get; init; }
    public bool IsCorrect { get; init; }
    public string? QuestionFeedbackText { get; init; }
    public string? SelectedAnswerFeedback { get; init; }
    public string? CorrectAnswerFeedback { get; init; }
    public List<AttemptAnswerOptionRow> Options { get; } = new();
}
