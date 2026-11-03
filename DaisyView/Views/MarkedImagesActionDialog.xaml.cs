using System.Windows;

namespace DaisyView.Views;

public enum MarkedImagesAction
{
    LeaveMarked,
    Delete,
    Move,
    MakeFavorites
}

public partial class MarkedImagesActionDialog : Window
{
    public MarkedImagesAction SelectedAction { get; private set; } = MarkedImagesAction.LeaveMarked;

    public MarkedImagesActionDialog(int markedCount)
    {
        InitializeComponent();
        MessageText.Text = $"{markedCount} image(s) are marked.";
    }

    private void LeaveMarked_Click(object sender, RoutedEventArgs e)
    {
        SelectedAction = MarkedImagesAction.LeaveMarked;
        DialogResult = true;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        SelectedAction = MarkedImagesAction.Delete;
        DialogResult = true;
    }

    private void Move_Click(object sender, RoutedEventArgs e)
    {
        SelectedAction = MarkedImagesAction.Move;
        DialogResult = true;
    }

    private void MakeFavorites_Click(object sender, RoutedEventArgs e)
    {
        SelectedAction = MarkedImagesAction.MakeFavorites;
        DialogResult = true;
    }
}
