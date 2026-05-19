using System.Collections.ObjectModel;
using MV_BL.Domain;
using MV_BL.Services;
using MV_WPF.Helpers;

namespace MV_WPF.ViewModels;

public class AnswerInput : ViewModelBase
{
	public int Id { get; set; } // 0 = new slot, >0 = existing Answer.Id

	private string _text = string.Empty;
	public string Text { get => _text; set => Set(ref _text, value); }

	private bool _isCorrect;
	public bool IsCorrect { get => _isCorrect; set => Set(ref _isCorrect, value); }

	private string? _feedback;
	public string? Feedback { get => _feedback; set => Set(ref _feedback, value); }
}

public class QuestionEditorViewModel : ViewModelBase
{
	private readonly TopicService    _topicService;
	private readonly QuestionService _questionService;

	public event Action? DataChanged;

	public ObservableCollection<Topic>       Topics      { get; } = new();
	public ObservableCollection<Question>    Questions   { get; } = new();
	public ObservableCollection<AnswerInput> EditAnswers { get; } = new();


	private readonly List<Question> _allQuestions = new();

	private readonly List<Question> _selectedQuestions = new();

    // filters
    private Topic? _selectedTopic;
	public Topic? SelectedTopic
	{
		get => _selectedTopic;
		set { if (Set(ref _selectedTopic, value)) ReloadQuestions(); }
	}

	private int? _difficultyFilter;
	public int? DifficultyFilter
	{
		get => _difficultyFilter;
		set { if (Set(ref _difficultyFilter, value)) ReloadQuestions(); }
	}

	private string _filterText = string.Empty;
	public string FilterText
	{
		get => _filterText;
		set { if (Set(ref _filterText, value)) ApplyFilter(); }
	}

	// Editor state 
	private Question? _selectedQuestion; // single selected question (for editor)
	public Question? SelectedQuestion
	{
		get => _selectedQuestion;
		set => Set(ref _selectedQuestion, value);
	}

	private bool _isEditing;
	public bool IsEditing
	{
		get => _isEditing;
		set { Set(ref _isEditing, value); OnPropertyChanged(nameof(ActionLabel)); }
	}
	public string ActionLabel => IsEditing ? "Update question" : "Add question";

	private string _questionText = string.Empty;
	public string QuestionText { get => _questionText; set => Set(ref _questionText, value); }

	private string? _questionFeedback;
	public string? QuestionFeedback { get => _questionFeedback; set => Set(ref _questionFeedback, value); }

	private int _difficulty = 1;
	public int Difficulty { get => _difficulty; set => Set(ref _difficulty, value); }

	private string _status = string.Empty;
	public string Status { get => _status; set => Set(ref _status, value); }

	// Commands
	public RelayCommand RefreshCommand       { get; }
	public RelayCommand SaveQuestionCommand  { get; }
	public RelayCommand DeactivateCommand    { get; }
	public RelayCommand ActivateCommand      { get; }
	public RelayCommand FlagCommand          { get; }
	public RelayCommand AddAnswerSlotCommand { get; }
	public RelayCommand ClearCommand         { get; }

	public QuestionEditorViewModel(TopicService topicService, QuestionService questionService)
	{
		_topicService    = topicService;
		_questionService = questionService;

		RefreshCommand       = new RelayCommand(_ => RefreshTopics());
		SaveQuestionCommand  = new RelayCommand(_ => SaveQuestion());
		DeactivateCommand    = new RelayCommand(_ => DeactivateSelected());
		ActivateCommand      = new RelayCommand(_ => ActivateSelected());
		FlagCommand          = new RelayCommand(_ => FlagSelected());
		AddAnswerSlotCommand = new RelayCommand(_ => EditAnswers.Add(new AnswerInput()));
		ClearCommand         = new RelayCommand(_ => ClearEditor());

		for (int i = 0; i < 4; i++) EditAnswers.Add(new AnswerInput());
		RefreshTopics();
	}


	public void SetSelectedQuestions(IEnumerable<object> items)
	{
		_selectedQuestions.Clear();
		_selectedQuestions.AddRange(items.OfType<Question>());

		var single = _selectedQuestions.Count == 1 ? _selectedQuestions[0] : null;
		if (!ReferenceEquals(_selectedQuestion, single))
		{
			_selectedQuestion = single;
			OnPropertyChanged(nameof(SelectedQuestion));
			LoadQuestionToEditor(single);
		}
	}

	public void RefreshTopics()
	{
		try
		{
			Topics.Clear();
			foreach (var t in _topicService.GetAll()) Topics.Add(t);
			ReloadQuestions();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	public void ReloadQuestions()
	{
		_allQuestions.Clear();
		try
		{
			IReadOnlyList<Question> qs = SelectedTopic is null
				? _questionService.GetAll()
				: _questionService.GetByTopic(SelectedTopic.Id, DifficultyFilter);
			_allQuestions.AddRange(qs);
			ApplyFilter();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	private void ApplyFilter()
	{
		Questions.Clear();
		var src = string.IsNullOrWhiteSpace(FilterText)
			? _allQuestions
			: _allQuestions.Where(q => q.QuestionText.Contains(FilterText, StringComparison.OrdinalIgnoreCase));
		foreach (var q in src) Questions.Add(q);
		Status = $"{Questions.Count} questions.";
	}

	private void LoadQuestionToEditor(Question? q)
	{
		if (q is null) { if (!IsEditing) return; ClearEditor(); return; }
		QuestionText     = q.QuestionText;
		QuestionFeedback = q.Feedback;
		Difficulty       = q.DifficultyLevel;
		EditAnswers.Clear();
		foreach (var a in q.Answers)
			EditAnswers.Add(new AnswerInput { Id = a.Id, Text = a.AnswerText, IsCorrect = a.IsCorrect, Feedback = a.Feedback });
		IsEditing = true;
	}

	private void ClearEditor()
	{
		QuestionText     = string.Empty;
		QuestionFeedback = null;
		Difficulty       = 1;
		EditAnswers.Clear();
		for (int i = 0; i < 4; i++) EditAnswers.Add(new AnswerInput());
		IsEditing         = false;
		_selectedQuestion = null;
		OnPropertyChanged(nameof(SelectedQuestion));
		Status = "Cleared.";
	}

	private void SaveQuestion()
	{
		try
		{
			if (IsEditing && _selectedQuestion is not null)
			{
				var answers = EditAnswers
					.Where(a => !string.IsNullOrWhiteSpace(a.Text))
					.Select(a => (a.Id, a.Text, a.IsCorrect, a.Feedback));
				_questionService.UpdateQuestion(_selectedQuestion.Id, QuestionText, Difficulty, answers, QuestionFeedback);
				Status = $"Updated question #{_selectedQuestion.Id}.";
			}
			else
			{
				if (SelectedTopic is null) { Status = "Pick a topic."; return; }
				var answers = EditAnswers
					.Where(a => !string.IsNullOrWhiteSpace(a.Text))
					.Select(a => (a.Text, a.IsCorrect, a.Feedback));
				var newId = _questionService.AddQuestion(SelectedTopic.Id, QuestionText, Difficulty, answers, QuestionFeedback);
				Status = $"Added question #{newId}.";
			}

			ClearEditor();
			ReloadQuestions();
			DataChanged?.Invoke();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	private void DeactivateSelected()
	{
		if (_selectedQuestions.Count == 0) { Status = "Select question(s)."; return; }
		try
		{
			foreach (var q in _selectedQuestions) _questionService.Deactivate(q.Id);
			Status = $"Deactivated {_selectedQuestions.Count} question(s).";
			ClearEditor();
			ReloadQuestions();
			DataChanged?.Invoke();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	private void ActivateSelected()
	{
		if (_selectedQuestions.Count == 0) { Status = "Select question(s)."; return; }
		try
		{
			foreach (var q in _selectedQuestions) _questionService.Activate(q.Id);
			Status = $"Activated {_selectedQuestions.Count} question(s).";
			ReloadQuestions();
			DataChanged?.Invoke();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}

	private void FlagSelected()
	{
		if (_selectedQuestions.Count == 0) { Status = "Select question(s)."; return; }
		try
		{
			foreach (var q in _selectedQuestions) _questionService.Flag(q.Id);
			Status = $"Soft-deleted {_selectedQuestions.Count} question(s).";
			ClearEditor();
			ReloadQuestions();
			DataChanged?.Invoke();
		}
		catch (Exception ex) { Status = "ERROR: " + ex.Message; }
	}
}
