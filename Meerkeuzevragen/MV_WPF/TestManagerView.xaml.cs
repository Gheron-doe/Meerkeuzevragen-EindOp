using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MV_BL.Domain;
using MV_BL.Services;
using MV_Util.Factories;

namespace MV_WPF;

public partial class TestManagerView : UserControl
{
    private TopicService?    _topicService;
    private QuestionService? _questionService;
    private TestService?     _testService;
    private AttemptService?  _attemptService;
    private UserService?     _userService;

    private readonly List<int> _selectedTopicIds = new();

    public event Action? DataChanged;

    public TestManagerView()
    {
        InitializeComponent();
    }


    public void Setup(TopicService topicSvc, QuestionService questionSvc,
        TestService testSvc, AttemptService attemptSvc, UserService userSvc)
    {
        _topicService    = topicSvc;
        _questionService = questionSvc;
        _testService     = testSvc;
        _attemptService  = attemptSvc;
        _userService     = userSvc;

        foreach (var format in ExporterFactory.AvailableFormats)
            FormatCombo.Items.Add(format);
        if (FormatCombo.Items.Count > 0)
            FormatCombo.SelectedIndex = 0;

        Refresh();
    }

    public void Refresh()
    {
        if (_topicService == null || _testService == null) return;

        try
        {
            _selectedTopicIds.Clear();

            var topics = _topicService.GetAll().ToList();
            TopicsCombo.ItemsSource = topics;

            var rows = new List<TestGridItem>();
            foreach (var test in _testService.GetAll())
                rows.Add(new TestGridItem(test));

            TestsGrid.ItemsSource = rows;
            StatusText.Text = $"{topics.Count} topics, {rows.Count} tests loaded.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Load error: " + ex.Message;
        }
    }

    private void OnGenerate(object sender, RoutedEventArgs e)
    {
        if (_questionService == null || _testService == null || _topicService == null) return;

        try
        {
            if (_selectedTopicIds.Count == 0)
            {
                StatusText.Text = "Select at least one topic.";
                return;
            }

            var allTopics      = _topicService.GetAll().ToList();
            var selectedTopics = allTopics.Where(t => _selectedTopicIds.Contains(t.Id)).ToList();

            string title = TitleBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                StatusText.Text = "Enter a test title.";
                return;
            }

            if (!int.TryParse(CountBox.Text, out int totalCount))
                totalCount = 10;

            int d1 = int.TryParse(Diff1Box.Text, out int v1) ? v1 : 0;
            int d2 = int.TryParse(Diff2Box.Text, out int v2) ? v2 : 0;
            int d3 = int.TryParse(Diff3Box.Text, out int v3) ? v3 : 0;

            var questionPool = _questionService.GetByTopicsAndDifficulties(_selectedTopicIds, null);

            var newTest = Test.Generate(title, questionPool, totalCount, d1, d2, d3);
            newTest.Topics = selectedTopics;

            _testService.Add(newTest);

            StatusText.Text = $"Test #{newTest.Id} created with {newTest.Questions.Count} questions.";
            DataChanged?.Invoke();
            Refresh();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Generate error: " + ex.Message;
        }
    }

    private void OnRefresh(object sender, RoutedEventArgs e) => Refresh();

    private void OnExport(object sender, RoutedEventArgs e)
    {
        if (_testService == null) return;

        if (TestsGrid.SelectedItem is not TestGridItem selected)
        {
            StatusText.Text = "Select a test first.";
            return;
        }

        string format = FormatCombo.SelectedItem?.ToString() ?? "txt";

        try
        {
            var dlg = new SaveFileDialog
            {
                FileName = $"{selected.Title}.{format}",
                Filter   = $"{format.ToUpperInvariant()} files|*.{format}|All files|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            var test = _testService.GetById(selected.Id);
            if (test == null) { StatusText.Text = "Test not found."; return; }

            bool includeAnswers = IncludeAnswersCheck.IsChecked == true;

            ExporterFactory.Create(format).Export(test, dlg.FileName, includeAnswers);

            StatusText.Text = $"Exported to {dlg.FileName}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Export error: " + ex.Message;
        }
    }

    private void OnRunTest(object sender, RoutedEventArgs e)
    {
        if (_testService == null || _attemptService == null || _userService == null) return;

        if (TestsGrid.SelectedItem is not TestGridItem selected)
        {
            StatusText.Text = "Select a test first.";
            return;
        }

        try
        {
            var test = _testService.GetById(selected.Id);
            if (test == null) { StatusText.Text = "Test not found."; return; }

            var dialog = new UsernameDialog();
            if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.Username))
                return;

            var user = _userService.GetByUsername(dialog.Username);
            if (user == null)
            {
                user = new AppUser(dialog.Username);
                _userService.Add(user);
            }

            var attempt = new TestAttempt(test.Id, user.Id);

            new TestRunnerWindow(test, attempt, _attemptService).ShowDialog();

            DataChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Run error: " + ex.Message;
        }
    }

    private void OnViewTest(object sender, RoutedEventArgs e)
    {
        if (_testService == null) return;

        if (TestsGrid.SelectedItem is not TestGridItem selected)
        {
            StatusText.Text = "Select a test first.";
            return;
        }

        try
        {
            var test = _testService.GetById(selected.Id);
            if (test == null) { StatusText.Text = "Test not found."; return; }
            new TestViewerWindow(test).Show();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Error: " + ex.Message;
        }
    }

    private void OnDeleteTest(object sender, RoutedEventArgs e)
    {
        if (_testService == null) return;

        if (TestsGrid.SelectedItem is not TestGridItem selected)
        {
            StatusText.Text = "Select a test first.";
            return;
        }

        try
        {
            _testService.Flag(selected.Id);
            StatusText.Text = "Test deleted.";
            DataChanged?.Invoke();
            Refresh();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Delete error: " + ex.Message;
        }
    }

    private void OnTopicChecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.Tag is int topicId)
            if (!_selectedTopicIds.Contains(topicId))
                _selectedTopicIds.Add(topicId);
    }
    private void OnTopicUnchecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.Tag is int topicId)
            _selectedTopicIds.Remove(topicId);
    }
}
