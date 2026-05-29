using System.Windows;
using System.Windows.Controls;
using MV_BL.Domain;
using MV_BL.Services;

namespace MV_WPF;

public partial class QuestionEditorView : UserControl
{
    private TopicService?    _topicService;
    private QuestionService? _questionService;

    private readonly List<int> _filterTopicIds = new();
    private readonly List<int> _filterDiffIds  = new();

    private readonly List<int> _editorTopicIds = new();

    private readonly List<(int AnswerId, TextBox TextBox, CheckBox CorrectBox, TextBox FeedbackBox)>
        _answerControls = new();

    private List<Question> _allQuestions = new();

    private List<Question> _selectedQuestions = new();

    private Question? _currentQuestion;

    private bool _isEditing;

    public event Action? DataChanged;

    public QuestionEditorView()
    {
        InitializeComponent();
    }

    public void Setup(TopicService topicSvc, QuestionService questionSvc)
    {
        _topicService    = topicSvc;
        _questionService = questionSvc;

        FilterDiffCombo.Items.Clear();
        for (int i = 1; i <= 3; i++)
        {
            int level = i;
            var cb = new CheckBox
            {
                Content = $"Level {level}",
                Margin  = new Thickness(4, 2, 4, 2)
            };
            cb.Checked   += (s, e) => { _filterDiffIds.Add(level);    LoadQuestions(); };
            cb.Unchecked += (s, e) => { _filterDiffIds.Remove(level); LoadQuestions(); };
            FilterDiffCombo.Items.Add(cb);
        }

        RefreshTopics();
    }

    public void RefreshTopics()
    {
        if (_topicService == null) return;
        try
        {
            var topics = _topicService.GetAll().ToList();

            FilterTopicsCombo.Items.Clear();
            _filterTopicIds.Clear();

            EditorTopicsPanel.Children.Clear();
            _editorTopicIds.Clear();

            foreach (var topic in topics)
            {
                var filterCb = new CheckBox
                {
                    Content = topic.Name,
                    Margin  = new Thickness(4, 2, 4, 2)
                };
                filterCb.Checked   += (s, e) => { _filterTopicIds.Add(topic.Id);    LoadQuestions(); };
                filterCb.Unchecked += (s, e) => { _filterTopicIds.Remove(topic.Id); LoadQuestions(); };
                FilterTopicsCombo.Items.Add(filterCb);

                var editorCb = new CheckBox
                {
                    Content = topic.Name,
                    Margin  = new Thickness(4, 2, 4, 2),
                    Tag     = topic.Id
                };
                editorCb.Checked   += (s, e) => _editorTopicIds.Add(topic.Id);
                editorCb.Unchecked += (s, e) => _editorTopicIds.Remove(topic.Id);
                EditorTopicsPanel.Children.Add(editorCb);
            }

            LoadQuestions();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Load error: " + ex.Message;
        }
    }

    private void LoadQuestions()
    {
        if (_questionService == null) return;
        try
        {
            var topicFilter = _filterTopicIds.Count == 0 ? null : (IEnumerable<int>?)_filterTopicIds;
            var diffFilter  = _filterDiffIds.Count  == 0 ? null : (IEnumerable<int>?)_filterDiffIds;

            _allQuestions = _questionService
                .GetByTopicsAndDifficulties(topicFilter?.ToList(), diffFilter?.ToList())
                .ToList();

            ApplySearch();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Load error: " + ex.Message;
        }
    }

    private void ApplySearch()
    {
        string term = FilterTextBox.Text;

        List<Question> filtered;
        if (string.IsNullOrWhiteSpace(term))
        {
            filtered = _allQuestions;
        }
        else
        {
            filtered = new List<Question>();
            foreach (var q in _allQuestions)
                if (q.QuestionText.Contains(term, StringComparison.OrdinalIgnoreCase))
                    filtered.Add(q);
        }

        QuestionsGrid.ItemsSource = filtered;
        StatusText.Text           = $"{filtered.Count} questions found.";
    }

    private void OnFilterTextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

    private void OnRefresh(object sender, RoutedEventArgs e) => RefreshTopics();

    private void OnQuestionSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedQuestions = QuestionsGrid.SelectedItems.Cast<Question>().ToList();

        var single = _selectedQuestions.Count == 1 ? _selectedQuestions[0] : null;

        if (!ReferenceEquals(_currentQuestion, single))
        {
            _currentQuestion = single;
            LoadQuestionIntoEditor(single);
        }
    }

    private void LoadQuestionIntoEditor(Question? question)
    {
        if (question == null)
        {
            if (!_isEditing) return;
            ClearEditor();
            return;
        }

        QuestionTextBox.Text     = question.QuestionText;
        QuestionFeedbackBox.Text = question.Feedback ?? string.Empty;
        DifficultyBox.Text       = question.DifficultyLevel.ToString();

        var assignedTopicIds = new HashSet<int>(question.Topics.Select(t => t.Id));
        foreach (CheckBox cb in EditorTopicsPanel.Children)
            if (cb.Tag is int id)
                cb.IsChecked = assignedTopicIds.Contains(id);

        AnswersPanel.Children.Clear();
        _answerControls.Clear();
        foreach (var answer in question.Answers)
            GenerateAnswerSlot(answer);

        _isEditing       = true;
        ActionLabel.Text = "Update question";
        SaveBtn.Content  = "Update question";
    }

    private void ClearEditor()
    {
        QuestionTextBox.Text     = string.Empty;
        QuestionFeedbackBox.Text = string.Empty;
        DifficultyBox.Text       = "1";

        foreach (CheckBox cb in EditorTopicsPanel.Children)
            cb.IsChecked = false;

        AnswersPanel.Children.Clear();
        _answerControls.Clear();
        for (int i = 0; i < 4; i++)
            GenerateAnswerSlot(null);

        _isEditing       = false;
        _currentQuestion = null;
        ActionLabel.Text = "Add question";
        SaveBtn.Content  = "Add question";
        StatusText.Text  = "Editor cleared.";
    }

    private void OnAddAnswerSlot(object sender, RoutedEventArgs e) => GenerateAnswerSlot(null);

    private void GenerateAnswerSlot(Answer? existingAnswer)
    {
        var outer      = new StackPanel { Margin = new Thickness(0, 5, 0, 6) };
        var row1       = new StackPanel { Orientation = Orientation.Horizontal };
        var correctBox = new CheckBox   { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
        var textBox    = new TextBox    { Width = 260, Padding = new Thickness(3) };
        row1.Children.Add(correctBox);
        row1.Children.Add(textBox);

        var row2        = new StackPanel { Orientation = Orientation.Horizontal };
        var fbLabel     = new TextBlock  { Text = "Feedback:", VerticalAlignment = VerticalAlignment.Center, FontSize = 11, Margin = new Thickness(24, 2, 5, 0) };
        var feedbackBox = new TextBox    { Width = 220, Padding = new Thickness(3), FontSize = 11 };
        row2.Children.Add(fbLabel);
        row2.Children.Add(feedbackBox);

        outer.Children.Add(row1);
        outer.Children.Add(row2);
        AnswersPanel.Children.Add(outer);

        if (existingAnswer != null)
        {
            correctBox.IsChecked = existingAnswer.IsCorrect;
            textBox.Text         = existingAnswer.AnswerText;
            feedbackBox.Text     = existingAnswer.Feedback ?? string.Empty;
            _answerControls.Add((existingAnswer.Id, textBox, correctBox, feedbackBox));
        }
        else
        {
            _answerControls.Add((0, textBox, correctBox, feedbackBox));
        }
    }

    private void OnClear(object sender, RoutedEventArgs e) => ClearEditor();

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (_questionService == null || _topicService == null) return;
        try
        {
            if (_editorTopicIds.Count == 0)
            {
                StatusText.Text = "Select at least one topic.";
                return;
            }

            if (!int.TryParse(DifficultyBox.Text, out int difficulty))
                difficulty = 1;

            var allTopics      = _topicService.GetAll().ToList();
            var selectedTopics = new List<Topic>();
            foreach (var topic in allTopics)
                if (_editorTopicIds.Contains(topic.Id))
                    selectedTopics.Add(topic);

            Question question;
            if (_isEditing && _currentQuestion != null)
                question = new Question(_currentQuestion.Id, QuestionTextBox.Text, difficulty);
            else
                question = new Question(QuestionTextBox.Text, difficulty);

            question.Feedback = string.IsNullOrWhiteSpace(QuestionFeedbackBox.Text) ? null : QuestionFeedbackBox.Text;
            question.Topics = selectedTopics;

            foreach (var ctrl in _answerControls)
            {
                if (string.IsNullOrWhiteSpace(ctrl.TextBox.Text)) continue;

                string text      = ctrl.TextBox.Text;
                bool   isCorrect = ctrl.CorrectBox.IsChecked == true;
                string feedback  = ctrl.FeedbackBox.Text ?? string.Empty;

                if (ctrl.AnswerId > 0)
                    question.Answers.Add(new Answer(ctrl.AnswerId, question.Id, text, isCorrect, feedback));
                else
                    question.Answers.Add(new Answer(text, isCorrect, feedback));
            }

            if (_isEditing && _currentQuestion != null)
            {
                _questionService.UpdateWithAnswersAndTopics(question, _editorTopicIds);
                StatusText.Text = $"Question #{question.Id} updated.";
            }
            else
            {
                _questionService.Add(question);
                StatusText.Text = $"Question #{question.Id} added.";
            }

            ClearEditor();
            LoadQuestions();
            DataChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Save error: " + ex.Message;
        }
    }

    private void OnActivate(object sender, RoutedEventArgs e)
    {
        if (_selectedQuestions.Count == 0) { StatusText.Text = "Select questions."; return; }
        foreach (var q in _selectedQuestions) q.IsActive = true;
        QuestionsGrid.Items.Refresh();
        StatusText.Text = $"{_selectedQuestions.Count} question(s) activated.";
    }

    private void OnDeactivate(object sender, RoutedEventArgs e)
    {
        if (_selectedQuestions.Count == 0) { StatusText.Text = "Select questions."; return; }
        foreach (var q in _selectedQuestions) q.IsActive = false;
        QuestionsGrid.Items.Refresh();
        StatusText.Text = $"{_selectedQuestions.Count} question(s) deactivated.";
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (_questionService == null) return;
        if (_selectedQuestions.Count == 0) { StatusText.Text = "Select questions."; return; }
        try
        {
            foreach (var q in _selectedQuestions)
                _questionService.Flag(q.Id);
            StatusText.Text = $"{_selectedQuestions.Count} question(s) deleted.";
            ClearEditor();
            LoadQuestions();
            DataChanged?.Invoke();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Delete error: " + ex.Message;
        }
    }
}
