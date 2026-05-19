using System.Collections.ObjectModel;
using Microsoft.Win32;
using MV_BL.Domain;
using MV_BL.Services;
using MV_Util.Factories;
using MV_WPF.Helpers;

namespace MV_WPF.ViewModels;

public class BulkImportResultRow
{
	public int    UserId       { get; init; }
	public int    CorrectCount { get; init; }
	public int    Total        { get; init; }
	public bool   Persisted    { get; init; }
	public string Wrong        { get; init; } = string.Empty;
}

public class ImportSourceViewModel : ViewModelBase
{
	private readonly TopicService           _topicService;
	private readonly ImportService          _importService;
	private readonly ImporterFactory        _importerFactory;
	private readonly AttemptService         _attemptService;
	private readonly BulkInputParserFactory _bulkFactory;
	private readonly TestService            _testService;

	public event Action? DataChanged;

	// Topic/question import 
	public ObservableCollection<Topic>  Topics  { get; } = new();
	public ObservableCollection<string> Formats { get; } = new();

	private Topic? _selectedTopic;
	public Topic? SelectedTopic { get => _selectedTopic; set => Set(ref _selectedTopic, value); }

	private string _newTopicName = string.Empty;
	public string NewTopicName { get => _newTopicName; set => Set(ref _newTopicName, value); }

	private string _selectedFormat = "txt";
	public string SelectedFormat { get => _selectedFormat; set => Set(ref _selectedFormat, value); }

	private string _importFilePath = string.Empty;
	public string ImportFilePath { get => _importFilePath; set => Set(ref _importFilePath, value); }

	private int _importDifficulty = 1;
	public int ImportDifficulty { get => _importDifficulty; set => Set(ref _importDifficulty, value); }

	public IReadOnlyList<int> DifficultyOptions { get; } = new[] { 1, 2, 3 };

	// Bulk attempt import 
	public ObservableCollection<Test>                BulkTests   { get; } = new();
	public ObservableCollection<string>              BulkFormats { get; } = new();
	public ObservableCollection<BulkImportResultRow> BulkResults { get; } = new();

	private Test? _bulkSelectedTest;
	public Test? BulkSelectedTest { get => _bulkSelectedTest; set => Set(ref _bulkSelectedTest, value); }

	private string _bulkSelectedFormat = "csv";
	public string BulkSelectedFormat { get => _bulkSelectedFormat; set => Set(ref _bulkSelectedFormat, value); }

	private string _bulkFilePath = string.Empty;
	public string BulkFilePath { get => _bulkFilePath; set => Set(ref _bulkFilePath, value); }

	private string _status = string.Empty;
	public string Status { get => _status; set => Set(ref _status, value); }

	public RelayCommand RefreshCommand       { get; }
	public RelayCommand AddTopicCommand      { get; }
	public RelayCommand PickFileCommand      { get; }
	public RelayCommand ImportCommand        { get; }
	public RelayCommand BulkPickFileCommand  { get; }
	public RelayCommand BulkImportCommand    { get; }

	public ImportSourceViewModel(
		TopicService topicService,
		ImportService importService,
		ImporterFactory importerFactory,
		AttemptService attemptService,
		BulkInputParserFactory bulkFactory,
		TestService testService)
	{
		_topicService    = topicService;
		_importService   = importService;
		_importerFactory = importerFactory;
		_attemptService  = attemptService;
		_bulkFactory     = bulkFactory;
		_testService     = testService;

		RefreshCommand      = new RelayCommand(_ => Refresh());
		AddTopicCommand     = new RelayCommand(_ => AddTopic());
		PickFileCommand     = new RelayCommand(_ => PickFile());
		ImportCommand       = new RelayCommand(_ => Import());
		BulkPickFileCommand = new RelayCommand(_ => BulkPickFile());
		BulkImportCommand   = new RelayCommand(_ => BulkImport());

		foreach (var f in _importerFactory.AvailableFormats) Formats.Add(f);
		foreach (var f in _bulkFactory.AvailableFormats)     BulkFormats.Add(f);
		Refresh();
	}

	// commands

	public void Refresh()
	{
		try
		{
			Topics.Clear();
			foreach (var t in _topicService.GetAll()) Topics.Add(t);
			BulkTests.Clear();
			foreach (var t in _testService.GetAll()) BulkTests.Add(t);
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	private void AddTopic()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(NewTopicName)) { Status = "Name required."; return; }
			var id = _topicService.AddTopic(NewTopicName);
			Status = $"Added topic #{id}.";
			NewTopicName = string.Empty;
			Refresh();
			DataChanged?.Invoke();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	private void PickFile()
	{
		var dlg = new OpenFileDialog { Filter = "Text files|*.txt|All files|*.*" };
		if (dlg.ShowDialog() == true) ImportFilePath = dlg.FileName;
	}

	private void Import()
	{
		try
		{
			if (SelectedTopic is null)                   { Status = "Pick a topic."; return; }
			if (string.IsNullOrWhiteSpace(ImportFilePath)) { Status = "Pick a file.";  return; }
			var importer = _importerFactory.Create(SelectedFormat);
			int n = _importService.ParseAndSeed(ImportFilePath, SelectedTopic.Id, importer, ImportDifficulty);
			Status = $"Imported {n} questions into '{SelectedTopic.Name}'.";
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	private void BulkPickFile()
	{
		var dlg = new OpenFileDialog
		{
			Title  = "Select bulk import CSV file",
			Filter = "CSV files|*.csv|All files|*.*"
		};
		if (dlg.ShowDialog() == true) BulkFilePath = dlg.FileName;
	}

	private void BulkImport()
	{
		try
		{
			if (BulkSelectedTest is null)             { Status = "Select a test."; return; }
			if (string.IsNullOrWhiteSpace(BulkFilePath)) { Status = "Pick a CSV file."; return; }

			var parser  = _bulkFactory.Create(BulkSelectedFormat);
			var results = _attemptService.ImportFromFile(BulkSelectedTest.Id, BulkFilePath, parser);

			BulkResults.Clear();
			foreach (var r in results)
			{
				var wrong = string.Join(", ", r.Feedback
					.Where(f => !f.IsCorrect)
					.Select(f => $"Q{f.QuestionOrder} (correct: {f.CorrectLetter})"));
				BulkResults.Add(new BulkImportResultRow
				{
					UserId       = r.UserId,
					CorrectCount = r.CorrectCount,
					Total        = r.Total,
					Persisted    = r.Persisted,
					Wrong        = wrong
				});
			}
			Status = $"Processed {BulkResults.Count} rows.";
			DataChanged?.Invoke();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}
}
