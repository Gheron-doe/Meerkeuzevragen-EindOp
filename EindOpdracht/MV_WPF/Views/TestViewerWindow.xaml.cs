using MV_BL.Domain;
using System.Windows;
using System.Windows.Media;

namespace MV_WPF.Views;

public class ViewerAnswer
{
	public required string Label    { get; set; }
	public required bool   IsCorrect { get; set; }
	public bool ShowCorrect { get; set; }

	public Brush  Color  => (IsCorrect && ShowCorrect) ? Brushes.Green : Brushes.Black;
	public string Weight => (IsCorrect && ShowCorrect) ? "Bold" : "Normal";
}

public class ViewerQuestion
{
	public required string          Header       { get; set; }
	public required string          QuestionText { get; set; }
	public required List<ViewerAnswer> Answers   { get; set; }
}

public partial class TestViewerWindow : Window
{
	private readonly Test _test;
	private bool _showCorrect;
	private List<ViewerQuestion> _questions = new();

	public TestViewerWindow(Test test)
	{
		InitializeComponent();
		_test = test;
		TestTitleBlock.Text = test.Title;
		RebuildQuestions();
	}

	private void OnToggle(object sender, RoutedEventArgs e)
	{
		_showCorrect = HighlightToggle.IsChecked == true;
		RebuildQuestions();
	}

	private void RebuildQuestions()
	{
		_questions = _test.Questions.OrderBy(tq => tq.QuestionOrder).Select(tq =>
		{
			var q = tq.Question!;
			var answers = new List<ViewerAnswer>();
			for (int i = 0; i < tq.AnswerDisplayOrder.Count; i++)
			{
				int origOrder = tq.AnswerDisplayOrder[i];
				var a = q.Answers.FirstOrDefault(x => x.OriginalOrder == origOrder);
				if (a is null) continue;
				char letter = (char)('A' + i);
				answers.Add(new ViewerAnswer
				{
					Label       = $"{letter}. {a.AnswerText}",
					IsCorrect   = a.IsCorrect,
					ShowCorrect = _showCorrect
				});
			}
			return new ViewerQuestion
			{
				Header       = $"Q{tq.QuestionOrder}",
				QuestionText = q.QuestionText,
				Answers      = answers
			};
		}).ToList();

		QuestionsPanel.ItemsSource = null;
		QuestionsPanel.ItemsSource = _questions;
	}
}
