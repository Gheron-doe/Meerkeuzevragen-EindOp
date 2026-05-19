using System.Windows;
using System.Windows.Input;

namespace MV_WPF.Views;

public partial class UsernameDialog : Window
{
	public string Username { get; private set; } = string.Empty;

	public UsernameDialog()
	{
		InitializeComponent();
		Loaded += (_, _) => UsernameBox.Focus();
	}

	private void OnProceed(object sender, RoutedEventArgs e) => Confirm();
	private void OnCancel(object sender, RoutedEventArgs e)  => DialogResult = false;
	private void OnKeyDown(object sender, KeyEventArgs e)
	{
		if (e.Key == Key.Enter) Confirm();
		if (e.Key == Key.Escape) DialogResult = false;
	}

	private void Confirm()
	{
		var name = UsernameBox.Text.Trim();
		if (string.IsNullOrWhiteSpace(name))
		{
			MessageBox.Show("Username cannot be empty.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
			return;
		}
		Username = name;
		DialogResult = true;
	}
}
