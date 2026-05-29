using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MV_BL.Domain;
using MV_BL.Services;
using MV_Util.Factories;

namespace MV_WPF;

public partial class ImportSourceView : UserControl
{
    private TopicService?   _topicService;
    private ImportService?  _importService;
    private AttemptService? _attemptService;
    private UserService?    _userService;
    private TestService?    _testService;

    private List<SelectableTopic> _importTopics = new();

    public event Action? DataChanged;

    public ImportSourceView()
    {
        InitializeComponent();
    }

    public void Setup(TopicService topicSvc, ImportService importSvc,
        AttemptService attemptSvc, UserService userSvc, TestService testSvc)
    {
        _topicService   = topicSvc;
        _importService  = importSvc;
        _attemptService = attemptSvc;
        _userService    = userSvc;
        _testService    = testSvc;

        // Question import formats (e.g. "txt") come from the factory.
        foreach (var format in ImporterFactory.AvailableFormats)
            ImportFormatCombo.Items.Add(format);
        if (ImportFormatCombo.Items.Count > 0)
            ImportFormatCombo.SelectedIndex = 0;

        // Bulk input formats (e.g. "csv").
        foreach (var format in BulkInputParserFactory.AvailableFormats)
            BulkFormatCombo.Items.Add(format);
        if (BulkFormatCombo.Items.Count > 0)
            BulkFormatCombo.SelectedIndex = 0;

        // Difficulty levels are fixed: 1, 2, 3.
        foreach (int level in new[] { 1, 2, 3 })
            ImportDiffCombo.Items.Add(level);
        ImportDiffCombo.SelectedIndex = 0;

        Refresh();
    }

    public void Refresh()
    {
        if (_topicService == null || _testService == null) return;
        try
        {
            _importTopics = new List<SelectableTopic>();
            foreach (var topic in _topicService.GetAll())
                _importTopics.Add(new SelectableTopic(topic));

            ImportTopicsCombo.ItemsSource = _importTopics;

            BulkTestCombo.ItemsSource  = _testService.GetAll().ToList();
            BulkTestCombo.SelectedItem = null;
        }
        catch (Exception ex)
        {
            StatusText.Text = "Load error: " + ex.Message;
        }
    }

    private void OnRefresh(object sender, RoutedEventArgs e) => Refresh();

    private void OnAddTopic(object sender, RoutedEventArgs e)
    {
        if (_topicService == null) return;

        string name = NewTopicBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText.Text = "Name is required.";
            return;
        }

        try
        {
            var topic = new Topic(name);
            _topicService.Add(topic);

            _importTopics.Add(new SelectableTopic(topic));

            ImportTopicsCombo.ItemsSource = null;
            ImportTopicsCombo.ItemsSource = _importTopics;

            StatusText.Text  = $"Topic #{topic.Id} added.";
            NewTopicBox.Text = string.Empty;
            DataChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Add error: " + ex.Message;
        }
    }

    private void OnPickFile(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Text files|*.txt|All files|*.*" };
        if (dlg.ShowDialog() == true)
            ImportFileBox.Text = dlg.FileName;
    }

    private void OnImport(object sender, RoutedEventArgs e)
    {
        if (_importService == null) return;
        try
        {
            var selectedTopics = new List<Topic>();
            foreach (var wrapper in _importTopics)
                if (wrapper.IsSelected)
                    selectedTopics.Add(wrapper.Topic);

            if (selectedTopics.Count == 0)
            {
                StatusText.Text = "Select at least one topic.";
                return;
            }
            if (string.IsNullOrWhiteSpace(ImportFileBox.Text))
            {
                StatusText.Text = "Choose a file.";
                return;
            }

            string format     = ImportFormatCombo.SelectedItem?.ToString() ?? "txt";
            int    difficulty = ImportDiffCombo.SelectedItem is int d ? d : 1;
            var    importer   = ImporterFactory.Create(format);

            var parsed = _importService.Parse(ImportFileBox.Text, importer);

            int saved = 0;
            foreach (var question in parsed)
            {
                question.DifficultyLevel = difficulty;
                question.Topics          = selectedTopics.ToList();
                _importService.Persist(new[] { question });
                saved++;
            }

            StatusText.Text = $"{saved} questions imported into {selectedTopics.Count} topic(s).";
            DataChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Import error: " + ex.Message;
        }
    }

    private void OnBulkPickFile(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Select bulk CSV file",
            Filter = "CSV files|*.csv|All files|*.*"
        };
        if (dlg.ShowDialog() == true)
            BulkFileBox.Text = dlg.FileName;
    }

    private void OnBulkImport(object sender, RoutedEventArgs e)
    {
        if (_attemptService == null || _userService == null || _testService == null) return;
        try
        {
            if (BulkTestCombo.SelectedItem is not Test selectedTest)
            {
                StatusText.Text = "Select a test.";
                return;
            }
            if (string.IsNullOrWhiteSpace(BulkFileBox.Text))
            {
                StatusText.Text = "Choose a CSV file.";
                return;
            }

            var test = _testService.GetById(selectedTest.Id)
                ?? throw new InvalidOperationException("Test not found.");

            string format = BulkFormatCombo.SelectedItem?.ToString() ?? "csv";
            var    parser = BulkInputParserFactory.Create(format);

            var batch      = new List<(TestAttempt Attempt, List<AttemptAnswer> Answers)>();
            var resultRows = new List<BulkImportResultRow>();
            int totalQ     = test.Questions.Count;

            foreach (var row in parser.Parse(BulkFileBox.Text))
            {
                var user      = _userService.GetById(row.UserId);
                bool canSave  = user != null;
                int  correct  = 0;
                string errors = string.Empty;

                if (canSave)
                {
                    var attempt = new TestAttempt(test.Id, user!.Id)
                        { CompletedAt = DateTime.Now, Feedback = row.Feedback };
                    var answers = TestAttempt.AnswersFromLetters(0, test, row.Answers);

                    batch.Add((attempt, answers));


                    foreach (var answer in answers)
                    {
                        var tq      = test.Questions.FirstOrDefault(t => t.Id == answer.TestQuestionId);
                        var correct_answer = tq?.Question?.Answers.FirstOrDefault(a => a.IsCorrect);
                        if (answer.SelectedAnswerId.HasValue && correct_answer != null
                            && answer.SelectedAnswerId.Value == correct_answer.Id)
                            correct++;
                    }

                    var wrongList = new List<string>();
                    foreach (var tq in test.Questions.OrderBy(t => t.SortOrder))
                    {
                        var answer       = answers.FirstOrDefault(x => x.TestQuestionId == tq.Id);
                        var correctAnswer = tq.Question?.Answers.FirstOrDefault(a => a.IsCorrect);
                        bool ok = answer?.SelectedAnswerId.HasValue == true && correctAnswer != null
                               && answer.SelectedAnswerId.Value == correctAnswer.Id;
                        if (!ok && correctAnswer != null)
                            wrongList.Add($"Q{tq.SortOrder} (correct: {tq.AnswerIdToLetter(correctAnswer.Id)})");
                    }
                    errors = string.Join(", ", wrongList);
                }

                resultRows.Add(new BulkImportResultRow
                {
                    UserId       = row.UserId,
                    CorrectCount = correct,
                    Total        = totalQ,
                    Persisted    = canSave,
                    Wrong        = errors
                });
            }

            _attemptService.BulkInsertAttempts(batch);

            BulkResultsGrid.ItemsSource = resultRows;
            StatusText.Text = $"{batch.Count}/{resultRows.Count} attempts imported.";
            DataChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Bulk import error: " + ex.Message;
        }
    }
}
