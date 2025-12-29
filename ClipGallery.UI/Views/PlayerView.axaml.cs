using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ClipGallery.UI.Views;

public partial class PlayerView : UserControl
{
    public PlayerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
