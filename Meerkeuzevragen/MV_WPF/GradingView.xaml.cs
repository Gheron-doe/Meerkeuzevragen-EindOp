using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using MV_BL.Domain;
using MV_BL.Services;

namespace MV_WPF;

public partial class GradingView : UserControl
{
    private TestService?    _testService;
    private AttemptService? _attemptService;
    private UserService?    _userService;

    private readonly List<(TestAttempt Attempt, Test Test)> _allData = new();

    private List<GradingRow> _currentRows = new();

    public GradingView()
    {
        InitializeComponent();
    }

    public void Setup(TestService testSvc, AttemptService attemptSvc,
        UserService userSvc, TopicService topicSvc)
    {
        _testService    = testSvc;
        _attemptService = attemptSvc;
        _userService    = userSvc;
        Refresh();
    }

    public void Refresh()
    {
        if (_testService == null || _attemptService == null) return;
        try
        {
            _allData.Clear();

            var tests = _testService.GetAll().ToDictionary(t => t.Id);

            foreach (var attempt in _attemptService.GetAll())
                if (tests.TryGetValue(attempt.TestId, out var test))
                    _allData.Add((attempt, test));

            FilterTestCombo.ItemsSource  = tests.Values.OrderBy(t => t.Title).ToList();
            FilterTestCombo.SelectedItem = null;

            ApplyFilter();
            StatusText.Text = $"{_allData.Count} attempts loaded.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Load error: " + ex.Message;
        }
    }

    private string GetUsername(int userId, Dictionary<int, string> cache)
    {
        if (!cache.TryGetValue(userId, out var name))
            cache[userId] = name = _userService!.GetById(userId)?.Username ?? $"#{userId}";
        return name;
    }

    private void ApplyFilter()
    {
        if (_userService == null) return;

        var usernameCache = new Dictionary<int, string>();

        var selectedDiffs = new HashSet<int>();
        if (Diff1Check.IsChecked == true) selectedDiffs.Add(1);
        if (Diff2Check.IsChecked == true) selectedDiffs.Add(2);
        if (Diff3Check.IsChecked == true) selectedDiffs.Add(3);

        string filterUsername = FilterUsernameBox.Text;
        string filterTopic    = FilterTopicBox.Text;
        var    filterTest     = FilterTestCombo.SelectedItem as Test;

        var rows = new List<GradingRow>();

        foreach (var (attempt, test) in _allData)
        {
            if (!string.IsNullOrWhiteSpace(filterUsername))
            {
                string name = GetUsername(attempt.UserId, usernameCache);
                if (!name.Contains(filterUsername, StringComparison.OrdinalIgnoreCase))
                    continue;
            }

            if (!string.IsNullOrWhiteSpace(filterTopic))
            {
                bool hasTopic = test.Topics.Any(tp =>
                    tp.Name.Contains(filterTopic, StringComparison.OrdinalIgnoreCase));
                if (!hasTopic) continue;
            }

            if (filterTest != null && test.Id != filterTest.Id)
                continue;

            if (selectedDiffs.Count > 0)
            {
                bool hasDiff = test.Questions.Any(tq =>
                    tq.Question != null && selectedDiffs.Contains(tq.Question.DifficultyLevel));
                if (!hasDiff) continue;
            }

            int    total   = test.Questions.Count;
            int    correct = attempt.CountCorrect(test);
            double pct     = total == 0 ? 0 : Math.Round(100.0 * correct / total, 1);

            rows.Add(new GradingRow
            {
                AttemptId      = attempt.Id,
                UserId         = attempt.UserId,
                Username       = GetUsername(attempt.UserId, usernameCache),
                TestId         = test.Id,
                TestTitle      = test.Title,
                TopicNames     = test.TopicsString(),
                Difficulties   = test.DifficultiesString(),
                CorrectCount   = correct,
                TotalQuestions = total,
                DisplayScore   = pct,
                StartedAt      = attempt.StartedAt,
                CompletedAt    = attempt.CompletedAt,
                Feedback       = attempt.Feedback
            });
        }

        _currentRows            = rows;
        ResultsGrid.ItemsSource = _currentRows;
    }

    private void OnFilterChanged(object sender, RoutedEventArgs e) => ApplyFilter();

    private void OnTestFilterChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

    private void OnClearFilters(object sender, RoutedEventArgs e)
    {
        FilterUsernameBox.Text       = string.Empty;
        FilterTopicBox.Text          = string.Empty;
        FilterTestCombo.SelectedItem = null;
        Diff1Check.IsChecked         = false;
        Diff2Check.IsChecked         = false;
        Diff3Check.IsChecked         = false;
    }

    private void OnRefresh(object sender, RoutedEventArgs e) => Refresh();

    private void OnOpenAttempt(object sender, RoutedEventArgs e)
    {
        if (_attemptService == null || _testService == null) return;
        if (ResultsGrid.SelectedItem is not GradingRow row) return;
        try
        {
            new AttemptViewerWindow(row.AttemptId, _attemptService, _testService).Show();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Open error: " + ex.Message;
        }
    }


    private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (_attemptService == null) return;
        if (e.EditAction != DataGridEditAction.Commit) return;
        if (e.Column.Header?.ToString() != "Feedback") return;
        if (e.Row.Item is not GradingRow row) return;

        var     tb    = e.EditingElement as TextBox;
        string? value = string.IsNullOrWhiteSpace(tb?.Text) ? null : tb.Text.Trim();

        row.Feedback = value;
        _attemptService.UpdateFeedback(row.AttemptId, value);
    }

    private void OnExportSelected(object sender, RoutedEventArgs e)
    {
        if (_testService == null || _attemptService == null) return;

        var selected = _currentRows.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusText.Text = "Tick at least one row to export.";
            return;
        }

        try
        {
            var dlg = new SaveFileDialog
            {
                Title    = "Export attempts as CSV",
                Filter   = "CSV files|*.csv|All files|*.*",
                FileName = $"attempts_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };
            if (dlg.ShowDialog() != true) return;

            using var writer = new StreamWriter(dlg.FileName);
            writer.WriteLine("AttemptId,UserId,Username,TestId,Test,Answers,Correct,Total,Score%,Started,Completed,Feedback");

            foreach (var r in selected)
            {
                var test    = _testService.GetById(r.TestId);
                var attempt = _attemptService.GetById(r.AttemptId);
                if (test == null || attempt == null) continue;

                string letters = attempt.ToLetterString(test);

                string fb    = (r.Feedback ?? string.Empty).Replace("\"", "\"\"");
                string title = r.TestTitle.Replace("\"", "\"\"");

                writer.WriteLine(
                    $"{r.AttemptId},{r.UserId},\"{r.Username}\",{r.TestId},\"{title}\"," +
                    $"{letters},{r.CorrectCount},{r.TotalQuestions},{r.DisplayScore:F1}," +
                    $"{r.StartedAt:yyyy-MM-dd HH:mm},{r.CompletedAt:yyyy-MM-dd HH:mm},\"{fb}\"");
            }

            StatusText.Text = $"{selected.Count} attempts exported to {dlg.FileName}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = "Export error: " + ex.Message;
        }
    }
}
