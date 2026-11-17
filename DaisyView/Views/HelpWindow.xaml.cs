using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace DaisyView.Views;

/// <summary>
/// Help window displaying application information and command documentation
/// </summary>
public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        
        // Set version from assembly
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version?.ToString() ?? "1.0.0";
        
        // Prevent Enter key from triggering any actions
        PreviewKeyDown += (s, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Return)
            {
                e.Handled = true;
            }
        };
    }

    /// <summary>
    /// Handles hyperlink navigation
    /// </summary>
    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open link: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Closes the help window
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Opens the logs folder in Windows Explorer
    /// </summary>
    private void OpenLogsButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var logsPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DaisyView",
                "logs");

            if (!System.IO.Directory.Exists(logsPath))
            {
                MessageBox.Show(
                    "No log files found yet. Logs are created when the application runs.",
                    "Logs Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = logsPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not open logs folder: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
