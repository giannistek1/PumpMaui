namespace PumpMaui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("ResultsPage", typeof(ResultsPage));
    }
}