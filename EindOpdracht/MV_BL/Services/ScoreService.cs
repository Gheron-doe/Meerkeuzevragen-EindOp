using MV_BL.Domain;
using MV_BL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MV_BL.Services;

public class GradingResult
{
    public int CorrectCount { get; init; }
    public int Total { get; init; }
    public List<AttemptAnswer> Answers { get; init; } = new();
    public List<QuestionFeedback> Feedback { get; init; } = new();
}

public class QuestionFeedback
{
    public int QuestionOrder { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public string? GivenLetter { get; init; }
    public string CorrectLetter { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }

    public string? QuestionFeedbackText { get; init; }
    public string? SelectedAnswerFeedback { get; init; }
    public string? CorrectAnswerFeedback { get; init; }
}
public class ScoreService
{
    public static GradingResult Grade(Test test, IReadOnlyList<(int testQuestionId, int? selectedAnswerId)> selections)
    {
        var selMap = selections.ToDictionary(s => s.testQuestionId, s => s.selectedAnswerId);
        var attemptAnswers = new List<AttemptAnswer>();
        var feedback = new List<QuestionFeedback>();

        var ordered = test.Questions.OrderBy(tq => tq.QuestionOrder).ToList();
        foreach (var tq in ordered)
        {
            var q = tq.Question!;
            selMap.TryGetValue(tq.Id, out int? selectedAnswerId);

            var correctAnswer = q.Answers.FirstOrDefault(a => a.IsCorrect);
            bool isCorrect = selectedAnswerId.HasValue
                && correctAnswer is not null
                && selectedAnswerId.Value == correctAnswer.Id;

            attemptAnswers.Add(new AttemptAnswer
            {
                TestQuestionId = tq.Id,
                SelectedAnswerId = selectedAnswerId,
                IsCorrect = isCorrect
            });

            Answer? selectedAnswer = selectedAnswerId.HasValue
                ? q.Answers.FirstOrDefault(a => a.Id == selectedAnswerId.Value)
                : null;

            string? givenLetter = null;
            if (selectedAnswer is not null)
            {
                int slot = tq.AnswerDisplayOrder.IndexOf(selectedAnswer.OriginalOrder);
                if (slot >= 0) givenLetter = ((char)('A' + slot)).ToString();
            }

            string correctLetter = "?";
            if (correctAnswer is not null)
            {
                int slot = tq.AnswerDisplayOrder.IndexOf(correctAnswer.OriginalOrder);
                if (slot >= 0) correctLetter = ((char)('A' + slot)).ToString();
            }

            feedback.Add(new QuestionFeedback
            {
                QuestionOrder = tq.QuestionOrder,
                QuestionText = q.QuestionText,
                GivenLetter = givenLetter,
                CorrectLetter = correctLetter,
                IsCorrect = isCorrect,
                QuestionFeedbackText = q.Feedback,
                SelectedAnswerFeedback = selectedAnswer?.Feedback,
                CorrectAnswerFeedback = correctAnswer?.Feedback
            });
        }

        return new GradingResult
        {
            CorrectCount = attemptAnswers.Count(a => a.IsCorrect),
            Total = ordered.Count,
            Answers = attemptAnswers,
            Feedback = feedback
        };
    }

    public static GradingResult GradeFromLetters(Test test, string letterString)
    {
        var ordered = test.Questions.OrderBy(tq => tq.QuestionOrder).ToList();
        var selections = new List<(int testQuestionId, int? selectedAnswerId)>();

        for (int i = 0; i < ordered.Count; i++)
        {
            var tq = ordered[i];
            var q = tq.Question!;

            int? answerId = null;
            if (i < letterString.Length)
            {
                char ch = char.ToUpperInvariant(letterString[i]);
                int slot = ch - 'A';
                if (slot >= 0 && slot < tq.AnswerDisplayOrder.Count)
                {
                    int origOrder = tq.AnswerDisplayOrder[slot];
                    var a = q.Answers.FirstOrDefault(x => x.OriginalOrder == origOrder);
                    answerId = a?.Id;
                }
            }
            selections.Add((tq.Id, answerId));
        }

        return Grade(test, selections);
    }

    public static string ToLetterString(Test test, IReadOnlyList<AttemptAnswer> answers)
    {
        var answerMap = answers.ToDictionary(a => a.TestQuestionId, a => a.SelectedAnswerId);
        var chars = new List<char>();

        foreach (var tq in test.Questions.OrderBy(tq => tq.QuestionOrder))
        {
            var q = tq.Question!;
            char ch = '?';
            if (answerMap.TryGetValue(tq.Id, out int? selectedId) && selectedId.HasValue)
            {
                var a = q.Answers.FirstOrDefault(x => x.Id == selectedId.Value);
                if (a is not null)
                {
                    int slot = tq.AnswerDisplayOrder.IndexOf(a.OriginalOrder);
                    if (slot >= 0) ch = (char)('A' + slot);
                }
            }
            chars.Add(ch);
        }
        return new string(chars.ToArray());
    }

    public static double ComputeDisplayScore(Test test, int correctCount, IScoringStrategy strategy)
    {
        int total = test.Questions.Count;
        return total == 0 ? 0 : strategy.Calculate(correctCount, total);
    }
}
