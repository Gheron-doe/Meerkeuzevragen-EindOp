using System.Windows;
using System.Windows.Input;

namespace MV_WPF;

public partial class UsernameDialog : Window
{
    public string? Username { get; private set; }

    public UsernameDialog()
    {
        InitializeComponent();
    }
    private void OnProceed(object sender, RoutedEventArgs e)
    {
        Confirm();
    }
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Confirm();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        Username = null;
        Close();
    }

    private void Confirm()
    {
        string input = UsernameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            MessageBox.Show("Please enter a username.", "Username required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Username = input;
        Close();
    }
}
