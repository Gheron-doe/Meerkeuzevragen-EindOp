using MV_BL.Domain;
using MV_BL.Services;
using System.Windows;

namespace MV_WPF;

public partial class TestRunnerWindow : Window
{
    private readonly Test           _test;
    private readonly TestAttempt    _attempt;
    private readonly AttemptService _attemptService;

    private readonly List<RunnerQuestion> _questions = new();

    public TestRunnerWindow(Test test, TestAttempt attempt, AttemptService attemptService)
    {
        InitializeComponent();

        _test           = test;
        _attempt        = attempt;
        _attemptService = attemptService;

        TestTitle.Text = test.Title;

        BuildQuestions();
        QuestionsPanel.ItemsSource = _questions;
    }

    private void BuildQuestions()
    {
        foreach (var testQuestion in _test.Questions.OrderBy(q => q.SortOrder))
        {
            if (testQuestion.Question is null) continue;

            var shuffled = testQuestion.GetShuffledAnswers();
            var options  = new List<AnswerOption>();

            for (int i = 0; i < shuffled.Count; i++)
            {
                options.Add(new AnswerOption
                {
                    Label     = $"{(char)('A' + i)}. {shuffled[i].AnswerText}",
                    AnswerId  = shuffled[i].Id,
                    GroupName = $"tq_{testQuestion.Id}"
                });
            }

            _questions.Add(new RunnerQuestion
            {
                Header         = $"Q{testQuestion.SortOrder}",
                QuestionText   = testQuestion.Question.QuestionText,
                TestQuestionId = testQuestion.Id,
                AnswerOptions  = options
            });
        }
    }


    private void OnSubmit(object sender, RoutedEventArgs e)
    {
        try
        {
            var answers = new List<AttemptAnswer>();
            foreach (var question in _questions)
            {
                var chosen = question.AnswerOptions.FirstOrDefault(o => o.IsSelected);
                answers.Add(new AttemptAnswer(question.TestQuestionId, chosen?.AnswerId));
            }

            _attemptService.Save(_attempt, answers);

            _attempt.Answers = answers;

            new TestResultWindow(_attempt, _test).Show();
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error submitting test: " + ex.Message, "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
