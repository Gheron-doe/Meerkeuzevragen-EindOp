using MV_BL.Domain;
using System.Windows;

namespace MV_WPF;

public partial class TestResultWindow : Window
{
    public TestResultWindow(TestAttempt attempt, Test test)
    {
        InitializeComponent();

        int    totalQuestions = test.Questions.Count;
        int    correctCount   = attempt.CountCorrect(test);
        double percentage     = totalQuestions == 0 ? 0 : Math.Round(100.0 * correctCount / totalQuestions, 1);

        ScoreText.Text    = $"{correctCount}/{totalQuestions}  ({percentage:F1}%)";
        FeedbackText.Text = string.IsNullOrWhiteSpace(attempt.Feedback)
            ? "(no grader feedback yet)"
            : attempt.Feedback;


        var answerMap = attempt.Answers.ToDictionary(a => a.TestQuestionId);

        var wrongQuestions = new List<ResultQuestionRow>();
        int wrongCount     = 0;

        foreach (var testQuestion in test.Questions.OrderBy(t => t.SortOrder))
        {
            if (testQuestion.Question is null) continue;

            answerMap.TryGetValue(testQuestion.Id, out var givenAnswer);

            var correctAnswer = testQuestion.Question.Answers.FirstOrDefault(a => a.IsCorrect);

            Answer? chosenAnswer = null;
            if (givenAnswer?.SelectedAnswerId.HasValue == true)
                chosenAnswer = testQuestion.Question.Answers
                    .FirstOrDefault(a => a.Id == givenAnswer.SelectedAnswerId!.Value);

            bool wasCorrect = chosenAnswer != null
                           && correctAnswer != null
                           && chosenAnswer.Id == correctAnswer.Id;

            if (wasCorrect) continue;

            wrongCount++;

            var row = new ResultQuestionRow
            {
                Order                  = testQuestion.SortOrder,
                QuestionText           = testQuestion.Question.QuestionText,
                IsSkipped              = givenAnswer?.SelectedAnswerId is null,
                QuestionFeedbackText   = testQuestion.Question.Feedback,
                SelectedAnswerFeedback = chosenAnswer?.Feedback,
                CorrectAnswerFeedback  = correctAnswer?.Feedback
            };

            var shuffled = testQuestion.GetShuffledAnswers();
            for (int i = 0; i < shuffled.Count; i++)
            {
                row.Options.Add(new ResultAnswerRow
                {
                    Label      = ((char)('A' + i)).ToString(),
                    AnswerText = shuffled[i].AnswerText,
                    IsUserPick = chosenAnswer != null && shuffled[i].Id == chosenAnswer.Id,
                    IsCorrect  = shuffled[i].IsCorrect,
                    Feedback   = shuffled[i].Feedback
                });
            }

            wrongQuestions.Add(row);
        }

        SectionHeader.Text = wrongCount == 0
            ? "All correct — well done!"
            : $"To review ({wrongCount} questions):";

        ReviewList.ItemsSource = wrongQuestions;
    }
}
