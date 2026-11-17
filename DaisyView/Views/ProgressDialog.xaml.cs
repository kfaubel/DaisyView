using System;
using System.Windows;

namespace DaisyView.Views;

/// <summary>
/// Progress dialog shown when loading large folders
/// Displays progress and allows user to cancel the operation
/// </summary>
public partial class ProgressDialog : Window
{
    public event EventHandler? CancelRequested;

    public ProgressDialog()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Updates the progress bar and text
    /// </summary>
    public void UpdateProgress(int current, int total, string? statusText = null)
    {
        if (current > total)
            current = total;

        ProgressBar.Value = total > 0 ? (current * 100) / total : 0;
        ProgressTextBlock.Text = $"{current} / {total}";

        if (!string.IsNullOrEmpty(statusText))
        {
            StatusTextBlock.Text = statusText;
        }
    }

    /// <summary>
    /// Sets just the status text without changing progress
    /// </summary>
    public void SetStatus(string statusText)
    {
        StatusTextBlock.Text = statusText;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        CancelButton.IsEnabled = false;
        CancelButton.Content = "Cancelling...";
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
