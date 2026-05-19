using System.Windows.Controls;
using MV_WPF.ViewModels;

namespace MV_WPF.Views;

public partial class QuestionEditorView : UserControl
{
	public QuestionEditorView() => InitializeComponent();

	private void QuestionsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (DataContext is QuestionEditorViewModel vm)
			vm.SetSelectedQuestions(QuestionsGrid.SelectedItems.Cast<object>());
	}
}
