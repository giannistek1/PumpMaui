using PumpMaui.Game;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PumpMaui;

public partial class SongSelectPage : ContentPage, INotifyPropertyChanged
{
    private SscSong? _selectedSong;
    private SscChart? _selectedChart;
    private double _scrollSpeed = GameConstants.DefaultScrollSpeed;
    private List<SscChart> _currentSortedCharts = []; // Keep track of sorted charts for picker

    public ObservableCollection<SongListItem> SongList { get; } = [];

    private bool _hasSelection;
    public bool HasSelection
    {
        get => _hasSelection;
        private set
        {
            if (_hasSelection == value) return;
            _hasSelection = value;
            OnPropertyChanged();
        }
    }

    public double ScrollSpeed
    {
        get => _scrollSpeed;
        set
        {
            if (Math.Abs(_scrollSpeed - value) < 0.01) return;
            _scrollSpeed = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ScrollSpeedText));
        }
    }

    public string ScrollSpeedText => $"{_scrollSpeed:F1}x";

    public SongSelectPage()
    {
        InitializeComponent();
        BindingContext = this;

        // Register the GamePage route for Shell navigation
        Routing.RegisterRoute("GamePage", typeof(GamePage));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (SongList.Count == 0)
        {
            await LoadPhoenixSongsAsync();
        }
    }

    private void OnScrollSpeedChanged(object sender, ValueChangedEventArgs e)
    {
        ScrollSpeed = e.NewValue;
        System.Diagnostics.Debug.WriteLine($"🎮 Scroll speed changed to: {ScrollSpeed:F1}x");
    }

    private async Task LoadPhoenixSongsAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("🎵 Loading all Phoenix songs...");

            var loadedCount = 0;

            foreach (var songPath in GameConstants.Songs)
            {
                try
                {
                    await using var stream = await FileSystem.OpenAppPackageFileAsync(songPath);
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();

                    var song = SscParser.Parse(content, songPath);
                    if (song.Charts.Count > 0)
                    {
                        AddSong(song);
                        loadedCount++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to load {songPath}: {ex.Message}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"🎵 Successfully loaded {loadedCount} Phoenix songs");

            // Show a message if no songs were loaded
            if (loadedCount == 0)
            {
                await DisplayAlert("No Songs", "No Phoenix songs could be loaded. Please check that the song files are included in your project.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to load Phoenix songs: {ex.Message}");
            await DisplayAlert("Error", $"Failed to load songs: {ex.Message}", "OK");
        }
    }

    private void AddSong(SscSong song)
    {
        var item = new SongListItem
        {
            Song = song,
            Title = song.Title,
            Artist = song.Artist,
            ChartSummary = GenerateChartSummary(song)
        };
        SongList.Add(item);
    }

    private void PopulateCharts(SscSong song)
    {
        ChartPicker.Items.Clear();
        _currentSortedCharts.Clear();
        SelectedChartBorder.IsVisible = false; // Hide initially

        if (song?.Charts == null || song.Charts.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("No charts available for this song.");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"🎵 Populating charts for '{song.Title}':");

        // Sort charts by StepType first, then by meter
        _currentSortedCharts = song.Charts
            .OrderBy(c => c.StepType?.ToLower() == "pump-single" ? 0 : 1) // Singles first
            .ThenBy(c => c.Meter)
            .ToList();

        foreach (var chart in _currentSortedCharts)
        {
            var displayText = GetChartDisplayText(chart);
            ChartPicker.Items.Add(displayText);
            System.Diagnostics.Debug.WriteLine($"   Added chart: {displayText} (StepType: '{chart.StepType}', Difficulty: '{chart.Difficulty}', Meter: {chart.Meter})");
        }

        if (ChartPicker.Items.Count > 0)
        {
            ChartPicker.SelectedIndex = 0;
            _selectedChart = _currentSortedCharts.First();
            System.Diagnostics.Debug.WriteLine($"   Selected first chart: {ChartPicker.Items[0]}");

            // Show the selected chart display
            UpdateSelectedChartDisplay();
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("   ⚠️ No charts to display");
        }
    }

    /// <summary>
    /// Generates display text for a chart using S/D notation (e.g., "S5", "D12")
    /// </summary>
    private static string GetChartDisplayText(SscChart chart)
    {
        var stepPrefix = chart.StepType?.ToLower() switch
        {
            "pump-single" => "S",
            "pump-double" => "D",
            _ => ""
        };

        if (!string.IsNullOrEmpty(stepPrefix))
        {
            return $"{stepPrefix}{chart.Meter}";
        }

        // Fallback for charts without recognized StepType
        return string.IsNullOrEmpty(chart.Difficulty)
            ? $"Level {chart.Meter}"
            : (chart.Meter > 0
                ? $"{chart.Difficulty} {chart.Meter}"
                : chart.Difficulty);
    }

    private static string GenerateChartSummary(SscSong song)
    {
        if (song.Charts.Count == 0)
            return "No charts available";

        // Group by StepType and get range for each
        var singleCharts = song.Charts
            .Where(c => c.StepType?.ToLower() == "pump-single" && c.Meter > 0)
            .Select(c => c.Meter)
            .OrderBy(d => d)
            .Distinct()
            .ToList();

        var doubleCharts = song.Charts
            .Where(c => c.StepType?.ToLower() == "pump-double" && c.Meter > 0)
            .Select(c => c.Meter)
            .OrderBy(d => d)
            .Distinct()
            .ToList();

        var summaryParts = new List<string>();

        if (singleCharts.Count > 0)
        {
            var sMin = singleCharts.First();
            var sMax = singleCharts.Last();
            summaryParts.Add(sMin == sMax ? $"S{sMin}" : $"S{sMin}-{sMax}");
        }

        if (doubleCharts.Count > 0)
        {
            var dMin = doubleCharts.First();
            var dMax = doubleCharts.Last();
            summaryParts.Add(dMin == dMax ? $"D{dMin}" : $"D{dMin}-{dMax}");
        }

        // Fallback for unknown step types
        var otherCharts = song.Charts
            .Where(c => c.StepType?.ToLower() != "pump-single" &&
                       c.StepType?.ToLower() != "pump-double" &&
                       c.Meter > 0)
            .Select(c => c.Meter)
            .OrderBy(d => d)
            .Distinct()
            .ToList();

        if (otherCharts.Count > 0)
        {
            var oMin = otherCharts.First();
            var oMax = otherCharts.Last();
            summaryParts.Add(oMin == oMax ? $"L{oMin}" : $"L{oMin}-{oMax}");
        }

        if (summaryParts.Count == 0)
            return $"{song.Charts.Count} chart(s)";

        return $"{string.Join(", ", summaryParts)} • {song.Charts.Count} chart(s)";
    }

    private async void OnLoadDemoClicked(object sender, EventArgs e)
    {
        await LoadPhoenixSongsAsync();
    }

    private async void OnOpenFolderClicked(object sender, EventArgs e)
    {
        try
        {
#if WINDOWS
            await DisplayAlert("Feature Not Available", "External folder support coming soon for Windows!", "OK");
#else
            await DisplayAlert("Feature Not Available", "External folder support coming soon!", "OK");
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to open folder picker: {ex.Message}", "OK");
        }
    }

    private void OnSongSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is SongListItem selectedItem)
        {
            _selectedSong = selectedItem.Song;
            SelectedTitleLabel.Text = _selectedSong.Title;
            SelectedArtistLabel.Text = _selectedSong.Artist;

            PopulateCharts(_selectedSong);
            HasSelection = true;
        }
        else
        {
            HasSelection = false;
            _selectedSong = null;
            _selectedChart = null;
            _currentSortedCharts.Clear();
            ChartPicker.Items.Clear();
            SelectedChartBorder.IsVisible = false;
        }
    }

    private void OnChartChanged(object sender, EventArgs e)
    {
        if (_selectedSong == null || ChartPicker.SelectedIndex < 0 || ChartPicker.SelectedIndex >= _currentSortedCharts.Count)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Chart changed but invalid selection. Index: {ChartPicker.SelectedIndex}, Charts count: {_currentSortedCharts.Count}");
            SelectedChartBorder.IsVisible = false;
            return;
        }

        _selectedChart = _currentSortedCharts[ChartPicker.SelectedIndex];
        System.Diagnostics.Debug.WriteLine($"📝 Chart changed to: {ChartPicker.Items[ChartPicker.SelectedIndex]} - Notes: {_selectedChart.Notes.Count}");

        // Update the selected chart display
        UpdateSelectedChartDisplay();
    }

    private void UpdateSelectedChartDisplay()
    {
        if (_selectedChart == null)
        {
            SelectedChartBorder.IsVisible = false;
            return;
        }

        // Display chart using S/D notation
        var chartNotation = GetChartDisplayText(_selectedChart);
        SelectedChartLevelLabel.Text = chartNotation;
        SelectedChartNotesLabel.Text = $"{_selectedChart.Notes.Count} notes";

        // Show note count for debugging
        System.Diagnostics.Debug.WriteLine($"📊 Chart {chartNotation} has {_selectedChart.Notes.Count} notes");

        SelectedChartBorder.IsVisible = true;
    }

    private async void OnPlayClicked(object sender, EventArgs e)
    {
        if (_selectedSong == null || _selectedChart == null)
        {
            await DisplayAlert("No Selection", "Please select a song and chart first.", "OK");
            return;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"🎮 Starting game with:");
            System.Diagnostics.Debug.WriteLine($"   Song: {_selectedSong.Title}");
            System.Diagnostics.Debug.WriteLine($"   Chart: {_selectedChart.Difficulty} {_selectedChart.Meter}");
            System.Diagnostics.Debug.WriteLine($"   Scroll Speed: {ScrollSpeed:F1}x");

            var gameData = new GameStartData
            {
                Song = _selectedSong,
                Chart = _selectedChart,
                ScrollSpeed = ScrollSpeed
            };

            var queryParams = new Dictionary<string, object>
            {
                { "songData", System.Text.Json.JsonSerializer.Serialize(gameData) }
            };

            await Shell.Current.GoToAsync("GamePage", queryParams);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error starting game: {ex.Message}");
            await DisplayAlert("Error", $"Failed to start game: {ex.Message}", "OK");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class SongListItem
{
    public required SscSong Song { get; set; }
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public required string ChartSummary { get; set; }
}

public class GameStartData
{
    public required SscSong Song { get; set; }
    public required SscChart Chart { get; set; }
    public double ScrollSpeed { get; set; } = GameConstants.DefaultScrollSpeed;
}