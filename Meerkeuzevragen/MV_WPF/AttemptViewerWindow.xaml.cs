using System.Windows;
using MV_BL.Domain;
using MV_BL.Services;

namespace MV_WPF;

public partial class AttemptViewerWindow : Window
{
    private readonly AttemptService _attemptService;
    private readonly int            _attemptId;

    public AttemptViewerWindow(int attemptId, AttemptService attemptService, TestService testService)
    {
        InitializeComponent();

        _attemptService = attemptService;
        _attemptId      = attemptId;

        var attempt = attemptService.GetById(attemptId)
            ?? throw new ArgumentException($"Attempt {attemptId} not found.");
        var test = testService.GetById(attempt.TestId)
            ?? throw new ArgumentException($"Test {attempt.TestId} not found.");

        int totalQuestions = test.Questions.Count;
        int correctCount   = attempt.CountCorrect(test);

        Title           = $"Attempt #{attemptId} — {test.Title}";
        TitleBlock.Text = Title;
        ScoreText.Text  = $"{correctCount}/{totalQuestions}";
        FeedbackBox.Text = attempt.Feedback ?? string.Empty;

        var answerMap = attempt.Answers.ToDictionary(a => a.TestQuestionId);

        var questionRows = new List<AttemptQuestionRow>();

        foreach (var testQuestion in test.Questions.OrderBy(q => q.SortOrder))
        {
            if (testQuestion.Question == null) continue;

            answerMap.TryGetValue(testQuestion.Id, out var givenAnswer);

            var shuffledAnswers = testQuestion.GetShuffledAnswers();
            var correctAnswer   = testQuestion.Question.Answers.FirstOrDefault(a => a.IsCorrect);

            Answer? chosenAnswer = null;
            if (givenAnswer?.SelectedAnswerId.HasValue == true)
                chosenAnswer = testQuestion.Question.Answers
                    .FirstOrDefault(a => a.Id == givenAnswer.SelectedAnswerId!.Value);

            bool wasCorrect = chosenAnswer != null
                           && correctAnswer != null
                           && chosenAnswer.Id == correctAnswer.Id;

            var questionRow = new AttemptQuestionRow
            {
                Order                  = testQuestion.SortOrder,
                QuestionText           = testQuestion.Question.QuestionText,
                IsSkipped              = givenAnswer?.SelectedAnswerId == null,
                IsCorrect              = wasCorrect,
                QuestionFeedbackText   = testQuestion.Question.Feedback,
                SelectedAnswerFeedback = chosenAnswer?.Feedback,
                CorrectAnswerFeedback  = correctAnswer?.Feedback
            };

            char label = 'A';
            foreach (var answer in shuffledAnswers)
            {
                questionRow.Options.Add(new AttemptAnswerOptionRow
                {
                    Label      = label++.ToString(),
                    AnswerText = answer.AnswerText,
                    IsUserPick = givenAnswer?.SelectedAnswerId == answer.Id,
                    IsCorrect  = answer.IsCorrect,
                    Feedback   = answer.Feedback
                });
            }

            questionRows.Add(questionRow);
        }

        QuestionsItemsControl.ItemsSource = questionRows;
    }

    private void OnFeedbackLostFocus(object sender, RoutedEventArgs e)
    {
        string text = FeedbackBox.Text.Trim();

        // Store null rather than an empty string to keep the database clean.
        _attemptService.UpdateFeedback(_attemptId, string.IsNullOrEmpty(text) ? null : text);
    }
}
