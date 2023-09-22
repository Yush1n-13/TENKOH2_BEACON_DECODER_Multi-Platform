using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Controls.ApplicationLifetimes;
using Newtonsoft.Json;


namespace TENKOH2_BEACON_DECODER_Multi_Platform
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            SetInitialControlStates();
        }

        // private void InitializeComponent()
        // {
        //     AvaloniaXamlLoader.Load(this);
        // }

        private int _selectedDataLength = 25;
        private string _TargetPath;
        private string _TargetFolderPath;

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _TargetPath = FilePathTextBox.Text;
            _TargetFolderPath = FolderPathTextBox.Text;  // Assuming you add this variable for folder path as previously suggested

            var config = new
            {
                targetString = PrefixTextBox.Text,
                ReferencedFilePath = _TargetPath,
                ReferencedFolderPath = _TargetFolderPath,
                extractedDataLength = _selectedDataLength,

            };

            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                {
                    DefaultMembersSearchFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance
                }
            };

            Console.WriteLine(config);
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented, settings);
            File.WriteAllText("UserSettings.json", json);
            Console.WriteLine(json);
            
            this.Close();
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists("UserSettings.json"))
            {
                File.Delete("UserSettings.json");
                LoadSettingsFromFile();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            var result = await dialog.ShowAsync(this);
            
            if (result != null && result.Length > 0)
            {
                FilePathTextBox.Text = result[0];
            }
        }

        private async void BrowseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync(this);

            if (!string.IsNullOrEmpty(result))
            {
                FolderPathTextBox.Text = result;
                FilePathTextBox.Text = System.IO.Path.Combine(result, $"fldigi{DateTime.Now:yyyyMMdd}.log");
            }
        }

        private async void SetInitialControlStates()
        {

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                //PrefixTextBox.IsEnabled = false;
                
                if (FolderRadioButton.IsChecked == true)
                {
                    FilePathTextBox.IsEnabled = false;
                    BrowseFileButton.IsEnabled = false;
                }
                else
                {
                    FilePathTextBox.IsEnabled = true;
                    BrowseFileButton.IsEnabled = true;
                }
            });
        }

        private void FileRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            FilePathTextBox.IsEnabled = true;
            BrowseFileButton.IsEnabled = true;
            FolderPathTextBox.IsEnabled = false;
            BrowseFolderButton.IsEnabled = false;

            FolderPathTextBox.Text = string.Empty;
        }

        private void FolderRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            FilePathTextBox.IsEnabled = false;
            BrowseFileButton.IsEnabled = false;
            FolderPathTextBox.IsEnabled = true;
            BrowseFolderButton.IsEnabled = true;

            FilePathTextBox.Text = string.Empty;
        }

        private void NominalModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            _selectedDataLength = 25;
        }

        private void JAMSATModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            _selectedDataLength = 37;
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            //ToggleSwitch_Unchecked(null,null);
            LoadSettingsFromFile();
        }

        private void LoadSettingsFromFile()
        {
            string jsonString;

            if (File.Exists("UserSettings.json"))
            {
                jsonString = File.ReadAllText("UserSettings.json");
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "TENKOH2_BEACON_DECODER_Multi_Platform.AppConfigure.json";

                using Stream stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    Console.WriteLine($"Resource {resourceName} not found.");
                    return;
                }
                using StreamReader reader = new StreamReader(stream);
                jsonString = reader.ReadToEnd();
            }

            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<AppConfig>(jsonString);
            
            PrefixTextBox.Text = config.targetString;
            FilePathTextBox.Text = config.ReferencedFilePath;
            FolderPathTextBox.Text = config.ReferencedFolderPath;

            if (config.extractedDataLength == 25)
            {
                NominalModeRadioButton.IsChecked = true;
            }
            else if (config.extractedDataLength == 37)
            {
                JAMSATModeRadioButton.IsChecked = true;
            }
        }
    }

}
