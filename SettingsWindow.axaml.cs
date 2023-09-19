using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;


namespace TENKOH2_BEACON_DECODER_Multi_Platform
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            
            // UI要素の参照を取得
            PathTextBox = this.FindControl<TextBox>("PathTextBox");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = (Application.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime).MainWindow as MainWindow;
            mainWindow.UpdateReferencedFilePath(PathTextBox.Text);
            this.Close();
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            var result = await dialog.ShowAsync(this);
            
            if (result != null && result.Length > 0)
            {
                PathTextBox.Text = result[0];
            }
        }

    }
}
