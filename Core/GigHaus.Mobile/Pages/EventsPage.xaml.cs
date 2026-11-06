namespace GigHaus.Mobile.Pages;

public partial class EventsPage : ContentPage
{
    public EventsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // TODO: Fetch events from API via HttpClient
        StatusLabel.Text = "No events found. Check back soon!";
    }
}

