using System.Collections.ObjectModel;
using Microsoft.Win32;
using MV_BL.Domain;
using MV_BL.Services;
using MV_Util.Factories;
using MV_BL.Interfaces;
using MV_WPF.Helpers;
using MV_WPF.Views;

namespace MV_WPF.ViewModels;

public class TestRow
{
	public Test   Test      { get; }
	public string TopicName { get; }

	public int      Id              => Test.Id;
	public string   Title           => Test.Title;
	public string   ScoringStrategy => Test.ScoringStrategy.ToString();
	public int?     Difficulty      => Test.Difficulty;
	public DateTime? CreatedAt      => Test.CreatedAt;

	public TestRow(Test test, string topicName) { Test = test; TopicName = topicName; }
}

public class TestManagerViewModel : ViewModelBase
{
	private readonly TopicService           _topicService;
	private readonly TestService            _testService;
	private readonly AttemptService         _attemptService;
	private readonly ExporterFactory        _exporterFactory;
	private readonly ScoringStrategyFactory _scoringFactory;

	public event Action? DataChanged;

	public ObservableCollection<Topic>   Topics      { get; } = new();
	public ObservableCollection<TestRow> Tests       { get; } = new();
	public ObservableCollection<string>  ExportFormats { get; } = new();
	public ObservableCollection<ScoringMode> ScoringModes { get; } = new();

	private Topic? _selectedTopic;
	public Topic? SelectedTopic
	{
		get => _selectedTopic;
		set => Set(ref _selectedTopic, value);
	}

	private TestRow? _selectedTest;
	public TestRow? SelectedTest
	{
		get => _selectedTest;
		set => Set(ref _selectedTest, value);
	}

	private int _questionCount = 10;
	public int QuestionCount
	{
		get => _questionCount;
		set => Set(ref _questionCount, value);
	}

	private string _newTestTitle = string.Empty;
	public string NewTestTitle
	{
		get => _newTestTitle;
		set => Set(ref _newTestTitle, value);
	}

	private int? _difficulty;
	public int? Difficulty { get => _difficulty; set => Set(ref _difficulty, value); }

	private ScoringMode _selectedScoringMode = ScoringMode.SimplePercent;
	public ScoringMode SelectedScoringMode { get => _selectedScoringMode; set => Set(ref _selectedScoringMode, value); }

	private string _selectedFormat = "txt";
	public string SelectedFormat { get => _selectedFormat; set => Set(ref _selectedFormat, value); }

	private bool _includeAnswers;
	public bool IncludeAnswers { get => _includeAnswers; set => Set(ref _includeAnswers, value); }

	private string _status = string.Empty;
	public string Status { get => _status; set => Set(ref _status, value); }

	public RelayCommand RefreshCommand    { get; }
	public RelayCommand GenerateCommand   { get; }
	public RelayCommand ExportCommand     { get; }
	public RelayCommand RunTestCommand    { get; }
	public RelayCommand ViewTestCommand   { get; }
	public RelayCommand DeleteTestCommand { get; }

	public TestManagerViewModel(
		TopicService topicService,
		TestService testService,
		AttemptService attemptService,
		ExporterFactory exporterFactory,
		ScoringStrategyFactory scoringFactory)
	{
		_topicService    = topicService;
		_testService     = testService;
		_attemptService  = attemptService;
		_exporterFactory = exporterFactory;
		_scoringFactory  = scoringFactory;

		RefreshCommand    = new RelayCommand(_ => Refresh());
		GenerateCommand   = new RelayCommand(_ => Generate());
		ExportCommand     = new RelayCommand(_ => Export());
		RunTestCommand    = new RelayCommand(_ => RunTest());
		ViewTestCommand   = new RelayCommand(_ => ViewTest());
		DeleteTestCommand = new RelayCommand(_ => DeleteTest());

		foreach (var f in _exporterFactory.AvailableFormats) ExportFormats.Add(f);
		foreach (var m in _scoringFactory.AvailableModes)    ScoringModes.Add(m);
		Refresh();
	}

	public void Refresh()
	{
		try
		{
			Topics.Clear();
			foreach (var t in _topicService.GetAll()) Topics.Add(t);
			var topicMap = Topics.ToDictionary(t => t.Id, t => t.Name);

			Tests.Clear();
			foreach (var t in _testService.GetAll())
			{
				string topicName = topicMap.TryGetValue(t.TopicId, out var n) ? n : $"#{t.TopicId}";
				Tests.Add(new TestRow(t, topicName));
			}
			Status = $"Loaded {Topics.Count} topics, {Tests.Count} tests.";
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	private void Generate()
	{
		try
		{
			if (SelectedTopic is null)              { Status = "Pick a topic.";     return; }
			if (string.IsNullOrWhiteSpace(NewTestTitle)) { Status = "Title required."; return; }

			var test = _testService.GenerateTest(SelectedTopic.Id, QuestionCount, NewTestTitle, Difficulty, SelectedScoringMode);
			var row  = new TestRow(test, SelectedTopic.Name);
			Tests.Insert(0, row);
			SelectedTest = row;
			Status = $"Generated test #{test.Id} with {test.Questions.Count} questions.";
			DataChanged?.Invoke();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	private void Export()
	{
		try
		{
			if (SelectedTest is null) { Status = "Pick a test."; return; }
			var dlg = new SaveFileDialog
			{
				FileName = $"{SelectedTest.Title}.{SelectedFormat}",
				Filter   = $"{SelectedFormat.ToUpperInvariant()} files|*.{SelectedFormat}|All files|*.*"
			};
			if (dlg.ShowDialog() != true) return;
			var exporter = _exporterFactory.Create(SelectedFormat);
			_testService.Export(SelectedTest.Test.Id, dlg.FileName, exporter, IncludeAnswers);
			Status = $"Exported to {dlg.FileName}.";
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	public void RunTest()
	{
		if (SelectedTest is null) { Status = "Select a test first."; return; }
		try
		{
			var test = _testService.GetById(SelectedTest.Test.Id);
			var dlg  = new UsernameDialog();
			if (dlg.ShowDialog() != true || string.IsNullOrWhiteSpace(dlg.Username)) return;

			var (attemptId, _) = _attemptService.StartAttempt(test.Id, dlg.Username);
			var win = new TestRunnerWindow(test, attemptId, _attemptService, _scoringFactory);
			win.ShowDialog();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	public void ViewTest()
	{
		if (SelectedTest is null) { Status = "Select a test first."; return; }
		try
		{
			var test = _testService.GetById(SelectedTest.Test.Id);
			new TestViewerWindow(test).Show();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	public void DeleteTest()
	{
		if (SelectedTest is null) { Status = "Select a test first."; return; }
		try
		{
			_testService.Deactivate(SelectedTest.Test.Id);
			Tests.Remove(SelectedTest);
			SelectedTest = null;
			Status = "Test deactivated.";
			DataChanged?.Invoke();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}
}
