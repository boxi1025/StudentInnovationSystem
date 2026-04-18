using System.Windows;

namespace StudentInnovation.WpfApp.Views;

public partial class RejectReasonDialog : Window
{
    public string ReasonText { get; private set; } = string.Empty;

    public RejectReasonDialog()
    {
        InitializeComponent();
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        ReasonText = ReasonBox.Text;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
