using MV_BL.Domain;
using MV_BL.Services;
using System.Windows;

namespace MV_WPF.Views;

public class ResultAnswerRow
{
	public string  Label      { get; init; } = string.Empty;
	public string  AnswerText { get; init; } = string.Empty;
	public bool    IsUserPick { get; init; }
	public bool    IsCorrect  { get; init; }
	public string? Feedback   { get; init; }
}

public class ResultQuestionRow
{
	public int    Order                 { get; init; }
	public string QuestionText         { get; init; } = string.Empty;
	public bool   IsSkipped            { get; init; }
	public string? QuestionFeedbackText  { get; init; }
	public string? SelectedAnswerFeedback { get; init; }
	public string? CorrectAnswerFeedback  { get; init; }
	public List<ResultAnswerRow> Options { get; } = new();
}

// Window

public partial class TestResultWindow : Window
{
	public TestResultWindow(TestAttempt attempt, GradingResult grading, double displayScore, Test test)
	{
		InitializeComponent();

		ScoreText.Text    = $"{grading.CorrectCount}/{grading.Total}  ({displayScore:F1}%)";
		FeedbackText.Text = string.IsNullOrWhiteSpace(attempt.Feedback)
			? "(no feedback yet)"
			: attempt.Feedback;

		// Only show wrong or unanswered questions
		var wrongFeedback = grading.Feedback.Where(f => !f.IsCorrect).ToList();
		int wrongCount    = wrongFeedback.Count;

		SectionHeader.Text = wrongCount == 0
			? "All answers correct — great work!"
			: $"Questions to review ({wrongCount}):";

		//  QuestionOrder → TestQuestion
		var tqByOrder = test.Questions.ToDictionary(tq => tq.QuestionOrder);

		var rows = new List<ResultQuestionRow>();
		foreach (var fb in wrongFeedback.OrderBy(f => f.QuestionOrder))
		{
			if (!tqByOrder.TryGetValue(fb.QuestionOrder, out var tq)) continue;
			var q = tq.Question;
			if (q is null) continue;

			bool isSkipped = fb.GivenLetter is null;

			// Map selected Answer.Id
			int? selectedId = null;
			if (!isSkipped && fb.GivenLetter is { Length: 1 } gl)
			{
				int slot = gl[0] - 'A';
				if (slot >= 0 && slot < tq.AnswerDisplayOrder.Count)
				{
					int origOrder = tq.AnswerDisplayOrder[slot];
					selectedId = q.Answers.FirstOrDefault(a => a.OriginalOrder == origOrder)?.Id;
				}
			}

			var qRow = new ResultQuestionRow
			{
				Order                  = fb.QuestionOrder,
				QuestionText           = fb.QuestionText,
				IsSkipped              = isSkipped,
				QuestionFeedbackText   = fb.QuestionFeedbackText,
				SelectedAnswerFeedback = fb.SelectedAnswerFeedback,
				CorrectAnswerFeedback  = fb.CorrectAnswerFeedback
			};

			for (int i = 0; i < tq.AnswerDisplayOrder.Count; i++)
			{
				int origOrder = tq.AnswerDisplayOrder[i];
				var ans = q.Answers.FirstOrDefault(a => a.OriginalOrder == origOrder);
				if (ans is null) continue;
				qRow.Options.Add(new ResultAnswerRow
				{
					Label      = ((char)('A' + i)).ToString(),
					AnswerText = ans.AnswerText,
					IsUserPick = selectedId.HasValue && ans.Id == selectedId.Value,
					IsCorrect  = ans.IsCorrect,
					Feedback   = ans.Feedback
				});
			}

			rows.Add(qRow);
		}

		ReviewList.ItemsSource = rows;
	}
}
