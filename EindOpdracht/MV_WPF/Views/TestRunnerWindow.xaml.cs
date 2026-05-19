using MV_BL.Domain;
using MV_BL.Services;
using MV_Util.Factories;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MV_WPF.Views;

// VM helpers 

public class AnswerOption : INotifyPropertyChanged
{
	public string   Label     { get; init; } = string.Empty;
	public int      AnswerId  { get; init; }
	public string   GroupName { get; init; } = string.Empty;

	private bool _isSelected;
	public bool IsSelected
	{
		get => _isSelected;
		set { _isSelected = value; OnPropertyChanged(); }
	}

	public event PropertyChangedEventHandler? PropertyChanged;
	private void OnPropertyChanged([CallerMemberName] string? name = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RunnerQuestionVm
{
	public string              Header       { get; init; } = string.Empty;
	public string              QuestionText { get; init; } = string.Empty;
	public int                 TestQuestionId { get; init; }
	public List<AnswerOption>  AnswerOptions  { get; init; } = new();
}

// Window

public partial class TestRunnerWindow : Window
{
	private readonly Test                  _test;
	private readonly int                   _attemptId;
	private readonly AttemptService        _attemptService;
	private readonly ScoringStrategyFactory _scoringFactory;
	private readonly List<RunnerQuestionVm> _questions = new();

	public TestRunnerWindow(Test test, int attemptId, AttemptService attemptService, ScoringStrategyFactory scoringFactory)
	{
		InitializeComponent();
		_test           = test;
		_attemptId      = attemptId;
		_attemptService = attemptService;
		_scoringFactory = scoringFactory;

		TestTitle.Text = test.Title;
		BuildQuestions();
		QuestionsPanel.ItemsSource = _questions;
	}

	private void BuildQuestions()
	{
		foreach (var tq in _test.Questions.OrderBy(q => q.QuestionOrder))
		{
			var q = tq.Question!;
			var options = new List<AnswerOption>();
			for (int i = 0; i < tq.AnswerDisplayOrder.Count; i++)
			{
				int origOrder = tq.AnswerDisplayOrder[i];
				var a = q.Answers.FirstOrDefault(x => x.OriginalOrder == origOrder);
				if (a is null) continue;
				char letter = (char)('A' + i);
				options.Add(new AnswerOption
				{
					Label     = $"{letter}. {a.AnswerText}",
					AnswerId  = a.Id,
					GroupName = $"tq_{tq.Id}"
				});
			}
			_questions.Add(new RunnerQuestionVm
			{
				Header         = $"Q{tq.QuestionOrder}",
				QuestionText   = q.QuestionText,
				TestQuestionId = tq.Id,
				AnswerOptions  = options
			});
		}
	}

	private void OnSubmit(object sender, RoutedEventArgs e)
	{
		var selections = _questions
			.Select(vm => (
				vm.TestQuestionId,
				(int?)vm.AnswerOptions.FirstOrDefault(o => o.IsSelected)?.AnswerId))
			.ToList();

		try
		{
			var (attempt, grading) = _attemptService.CompleteAttempt(_attemptId, _test.Id, selections);
			var strategy = _scoringFactory.Create(_test.ScoringStrategy);
			int total = grading.Total > 0 ? grading.Total : 1;
			double display = strategy.Calculate(grading.CorrectCount, total);

			var resultWin = new TestResultWindow(attempt, grading, display, _test);
			resultWin.Show();
			Close();
		}
		catch (Exception ex)
		{
			MessageBox.Show("ERROR: " + ex.Message, "Submit failed", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}
}
