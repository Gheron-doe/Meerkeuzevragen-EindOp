using System.Windows;
using MV_BL.Services;
using MV_WPF.ViewModels;

namespace MV_WPF.Views;

public partial class AttemptViewerWindow : Window
{
	public AttemptViewerWindow(int attemptId, AttemptService attemptService, TestService testService)
	{
		InitializeComponent();
		DataContext = new AttemptViewerViewModel(attemptId, attemptService, testService);
	}
}
