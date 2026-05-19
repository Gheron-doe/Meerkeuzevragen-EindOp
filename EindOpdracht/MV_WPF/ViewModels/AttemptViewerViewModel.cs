using MV_BL.Domain;
using MV_BL.Services;
using System.Collections.ObjectModel;

namespace MV_WPF.ViewModels;

public class AttemptAnswerOptionRow
{
	public string  Label      { get; init; } = string.Empty; // "A", "B", …
	public string  AnswerText { get; init; } = string.Empty;
	public bool    IsUserPick { get; init; }
	public bool    IsCorrect  { get; init; }
	public string? Feedback   { get; init; }
}

public class AttemptQuestionRow
{
	public int    Order        { get; init; }
	public string QuestionText { get; init; } = string.Empty;
	public bool   IsSkipped    { get; init; }
	public bool   IsCorrect    { get; init; }
	public string? QuestionFeedbackText    { get; init; }
	public string? SelectedAnswerFeedback  { get; init; }
	public string? CorrectAnswerFeedback   { get; init; }
	public ObservableCollection<AttemptAnswerOptionRow> Options { get; } = new();
}

public class AttemptViewerViewModel : ViewModelBase
{
	private readonly AttemptService _attemptService;
	private readonly int            _attemptId;

	public string Title     { get; }
	public string ScoreText { get; }

	public ObservableCollection<AttemptQuestionRow> Questions { get; } = new();

	private string? _feedback;
	public string? Feedback
	{
		get => _feedback;
		set
		{
			if (Set(ref _feedback, value))
				_attemptService.SetFeedback(_attemptId, value);
		}
	}

	public AttemptViewerViewModel(int attemptId, AttemptService attemptService, TestService testService)
	{
		_attemptService = attemptService;
		_attemptId      = attemptId;

		var attempt = attemptService.GetById(attemptId)
			?? throw new ArgumentException($"Attempt {attemptId} not found.");
		var test = testService.GetById(attempt.TestId);

		Title     = $"Attempt #{attemptId} — {test.Title}";
		int total = test.Questions.Count;
		ScoreText = attempt.Score.HasValue ? $"{attempt.Score}/{total}" : $"0/{total}";
		_feedback = attempt.Feedback;

		// Map TestQuestionId → AttemptAnswer for O(1) lookup
		var answerMap = attempt.Answers.ToDictionary(a => a.TestQuestionId);

		foreach (var tq in test.Questions.OrderBy(q => q.QuestionOrder))
		{
			answerMap.TryGetValue(tq.Id, out var attemptAns);
			var allAnswers = tq.Question?.Answers ?? new List<Answer>();

			// Rebuild the display order
			List<Answer> ordered;
			if (tq.AnswerDisplayOrder.Count > 0)
			{
				ordered = tq.AnswerDisplayOrder
					.Where(i => allAnswers.Any(a => a.OriginalOrder == i))
					.Select(i => allAnswers.First(a => a.OriginalOrder == i))
					.ToList();
			}
			else
			{
				ordered = allAnswers.OrderBy(a => a.OriginalOrder).ToList();
			}

			// Resolve feedback texts
			Answer? selectedAnswer = attemptAns?.SelectedAnswerId.HasValue == true
				? allAnswers.FirstOrDefault(a => a.Id == attemptAns.SelectedAnswerId!.Value)
				: null;
			Answer? correctAnswer  = allAnswers.FirstOrDefault(a => a.IsCorrect);

			var qRow = new AttemptQuestionRow
			{
				Order                  = tq.QuestionOrder,
				QuestionText           = tq.Question?.QuestionText ?? "(question not found)",
				IsSkipped              = attemptAns?.SelectedAnswerId is null,
				IsCorrect              = attemptAns?.IsCorrect ?? false,
				QuestionFeedbackText   = tq.Question?.Feedback,
				SelectedAnswerFeedback = selectedAnswer?.Feedback,
				CorrectAnswerFeedback  = correctAnswer?.Feedback
			};

			char label = 'A';
			foreach (var ans in ordered)
			{
				qRow.Options.Add(new AttemptAnswerOptionRow
				{
					Label      = label++.ToString(),
					AnswerText = ans.AnswerText,
					IsUserPick = attemptAns?.SelectedAnswerId == ans.Id,
					IsCorrect  = ans.IsCorrect,
					Feedback   = ans.Feedback
				});
			}

			Questions.Add(qRow);
		}
	}
}
