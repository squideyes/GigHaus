namespace GigHaus.Mobile.Pages;

public partial class CheckInPage : ContentPage
{
    public CheckInPage()
    {
        InitializeComponent();
    }

    private async void OnCheckInClicked(object? sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            ResultLabel.Text = "Please enter your email address.";
            ResultLabel.TextColor = Colors.Red;
            ResultLabel.IsVisible = true;
            return;
        }

        // TODO: Call API to check in by email
        ResultLabel.Text = $"Check-in for {email} — feature coming soon!";
        ResultLabel.TextColor = Colors.Green;
        ResultLabel.IsVisible = true;
    }
}

