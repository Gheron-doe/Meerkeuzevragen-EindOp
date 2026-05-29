using System.Windows.Media;

namespace MV_WPF;

public class ViewerAnswer
{
    public string Label { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
    public bool ShowCorrect { get; init; }
    public Brush Color  => (IsCorrect && ShowCorrect) ? Brushes.Green : Brushes.Black;
    public string Weight => (IsCorrect && ShowCorrect) ? "Bold"  : "Normal";
}
