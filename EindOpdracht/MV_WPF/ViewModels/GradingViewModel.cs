using System.Collections.ObjectModel;
using Microsoft.Win32;
using MV_BL.Domain;
using MV_BL.Services;
using MV_Util.Factories;
using MV_WPF.Helpers;
using MV_WPF.Views;

namespace MV_WPF.ViewModels;

public class GradingRow : ViewModelBase
{
	private readonly AttemptService _attemptService;

	public int       AttemptId      { get; init; }
	public int       UserId         { get; init; }
	public string    Username       { get; init; } = string.Empty;
	public int       TestId         { get; init; }
	public string    TestTitle      { get; init; } = string.Empty;
	public string    TopicName      { get; init; } = string.Empty;
	public int?      CorrectCount   { get; init; }
	public int       TotalQuestions { get; init; }
	public double?   DisplayScore   { get; init; }
	public string    ScoreText      => DisplayScore.HasValue ? $"{DisplayScore:F1}%" : "-";
	public DateTime  StartedAt      { get; init; }
	public DateTime? CompletedAt    { get; init; }

	private string? _feedback;
	public string? Feedback
	{
		get => _feedback;
		set { if (Set(ref _feedback, value)) _attemptService.SetFeedback(AttemptId, value); }
	}

	public void InitFeedback(string? feedback) => _feedback = feedback;

	public GradingRow(AttemptService svc) => _attemptService = svc;
}

public class GradingViewModel : ViewModelBase
{
	private readonly TestService            _testService;
	private readonly AttemptService         _attemptService;
	private readonly UserService            _userService;
	private readonly TopicService           _topicService;
	private readonly ScoringStrategyFactory _scoringFactory;

	private List<(TestAttempt attempt, Test test)> _allData = new();

	public ObservableCollection<GradingRow> Results     { get; } = new();
	public ObservableCollection<Test>       ExportTests { get; } = new();

	// filters 

	private string _filterUsername = string.Empty;
	public string FilterUsername
	{
		get => _filterUsername;
		set { Set(ref _filterUsername, value); SafeApplyFilter(); }
	}

	private string _filterTopic = string.Empty;
	public string FilterTopic
	{
		get => _filterTopic;
		set { Set(ref _filterTopic, value); SafeApplyFilter(); }
	}

	private DateTime? _filterStartDate;
	public DateTime? FilterStartDate
	{
		get => _filterStartDate;
		set { Set(ref _filterStartDate, value); SafeApplyFilter(); }
	}

	// Selection

	private GradingRow? _selectedResult;
	public GradingRow? SelectedResult
	{
		get => _selectedResult;
		set => Set(ref _selectedResult, value);
	}

	private Test? _exportTest;
	public Test? ExportTest { get => _exportTest; set => Set(ref _exportTest, value); }

	private string _status = string.Empty;
	public string Status { get => _status; set => Set(ref _status, value); }

	public RelayCommand RefreshCommand     { get; }
	public RelayCommand ExportCommand      { get; }
	public RelayCommand OpenAttemptCommand { get; }
	public RelayCommand ClearFiltersCommand { get; }

	public GradingViewModel(
		TestService testService,
		AttemptService attemptService,
		UserService userService,
		TopicService topicService,
		ScoringStrategyFactory scoringFactory)
	{
		_testService    = testService;
		_attemptService = attemptService;
		_userService    = userService;
		_topicService   = topicService;
		_scoringFactory = scoringFactory;

		RefreshCommand      = new RelayCommand(_ => Refresh());
		ExportCommand       = new RelayCommand(_ => Export());
		OpenAttemptCommand  = new RelayCommand(_ => OpenAttempt(), _ => SelectedResult is not null);
		ClearFiltersCommand = new RelayCommand(_ => ClearFilters());

		Refresh();
	}

	// commands

	public void Refresh()
	{
		try
		{
			_allData.Clear();
			var tests   = _testService.GetAll().ToDictionary(t => t.Id);
			var attempts = _attemptService.GetAll();

			foreach (var a in attempts)
				if (tests.TryGetValue(a.TestId, out var t)) _allData.Add((a, t));

			ExportTests.Clear();
			foreach (var t in tests.Values.OrderBy(t => t.Title)) ExportTests.Add(t);

			ApplyFilter();
			Status = $"Loaded {_allData.Count} attempts.";
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	private void SafeApplyFilter()
	{
		try { ApplyFilter(); }
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	private void ApplyFilter()
	{
		// Caches for name lookups (avoid repeated service calls)
		var userCache  = new Dictionary<int, string>();
		var topicCache = new Dictionary<int, string>();

		string GetUsername(int userId)
		{
			if (!userCache.TryGetValue(userId, out var name))
				userCache[userId] = name = _userService.GetById(userId)?.Username ?? $"#{userId}";
			return name;
		}

		string GetTopicName(int topicId)
		{
			if (!topicCache.TryGetValue(topicId, out var name))
			{
				try { name = _topicService.GetById(topicId).Name; }
				catch { name = $"#{topicId}"; }
				topicCache[topicId] = name;
			}
			return name;
		}

		Results.Clear();
		var q = _allData.AsEnumerable();

		if (!string.IsNullOrWhiteSpace(FilterUsername))
			q = q.Where(r => GetUsername(r.attempt.UserId).Contains(FilterUsername, StringComparison.OrdinalIgnoreCase));
		if (!string.IsNullOrWhiteSpace(FilterTopic))
			q = q.Where(r => GetTopicName(r.test.TopicId).Contains(FilterTopic, StringComparison.OrdinalIgnoreCase));
		if (FilterStartDate.HasValue)
			q = q.Where(r => r.attempt.StartedAt.Date == FilterStartDate.Value.Date);

		foreach (var (attempt, test) in q)
		{
			var full = _attemptService.GetById(attempt.Id);
			int total = full?.Answers.Count > 0 ? full.Answers.Count : Math.Max(1, attempt.Score ?? 1);

			var strategy = _scoringFactory.Create(test.ScoringStrategy);
			double? display = attempt.Score.HasValue ? strategy.Calculate(attempt.Score.Value, total) : null;

			string username  = GetUsername(attempt.UserId);
			string topicName = GetTopicName(test.TopicId);

			var row = new GradingRow(_attemptService)
			{
				AttemptId      = attempt.Id,
				UserId         = attempt.UserId,
				Username       = username,
				TestId         = test.Id,
				TestTitle      = test.Title,
				TopicName      = topicName,
				CorrectCount   = attempt.Score,
				TotalQuestions = total,
				DisplayScore   = display,
				StartedAt      = attempt.StartedAt,
				CompletedAt    = attempt.CompletedAt
			};
			row.InitFeedback(attempt.Feedback);
			Results.Add(row);
		}
	}

	private void ClearFilters()
	{
		FilterUsername  = string.Empty;
		FilterTopic     = string.Empty;
		FilterStartDate = null;
	}

	private void OpenAttempt()
	{
		if (SelectedResult is null) return;
		try
		{
			var win = new AttemptViewerWindow(SelectedResult.AttemptId, _attemptService, _testService);
			win.Show();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	private void Export()
	{
		try
		{
			if (ExportTest is null) { Status = "Select a test for export."; return; }
			var dlg = new SaveFileDialog
			{
				Title    = "Export test attempts as CSV",
				Filter   = "CSV files|*.csv|All files|*.*",
				FileName = $"{ExportTest.Title}_attempts.csv"
			};
			if (dlg.ShowDialog() != true) return;

			_attemptService.ExportAttemptsToCsv(ExportTest.Id, dlg.FileName);
			Status = $"Exported to {dlg.FileName}.";
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}
}
