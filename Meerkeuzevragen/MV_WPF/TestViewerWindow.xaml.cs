using MV_BL.Domain;
using System.Windows;

namespace MV_WPF;

// Shows the full content of a test: all questions with their answer options.
// The admin can toggle a switch to highlight the correct answers in green.
//
// Why a separate window and not a tab?
//   A separate window can stay open beside the main window, letting the admin
//   review test content while continuing other tasks.
//   Alternative: an extra tab in MainWindow — but that blocks the rest of the UI.
public partial class TestViewerWindow : Window
{
    private readonly Test _test;

    private bool _highlightOn;

    private List<ViewerQuestion> _questions = new();

    public TestViewerWindow(Test test)
    {
        InitializeComponent();
        _test = test;
        TestTitleBlock.Text = test.Title;

        BuildQuestions();
    }

    private void OnToggle(object sender, RoutedEventArgs e)
    {
        _highlightOn = HighlightToggle.IsChecked == true;
        BuildQuestions();
    }

    private void BuildQuestions()
    {
        _questions = new List<ViewerQuestion>();

        foreach (var testQuestion in _test.Questions.OrderBy(q => q.SortOrder))
        {
            if (testQuestion.Question is null)
            {
                _questions.Add(new ViewerQuestion
                {
                    Header       = $"Q{testQuestion.SortOrder}",
                    QuestionText = "(missing question content)"
                });
                continue;
            }

            var shuffled = testQuestion.GetShuffledAnswers();
            var answers  = new List<ViewerAnswer>();

            for (int i = 0; i < shuffled.Count; i++)
            {
                answers.Add(new ViewerAnswer
                {
                    Label       = $"{(char)('A' + i)}. {shuffled[i].AnswerText}",
                    IsCorrect   = shuffled[i].IsCorrect,
                    ShowCorrect = _highlightOn
                });
            }

            _questions.Add(new ViewerQuestion
            {
                Header       = $"Q{testQuestion.SortOrder}",
                QuestionText = testQuestion.Question.QuestionText,
                Answers      = answers
            });
        }

        QuestionsPanel.ItemsSource = null;
        QuestionsPanel.ItemsSource = _questions;
    }
}
