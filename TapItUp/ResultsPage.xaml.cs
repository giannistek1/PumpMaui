using TapItUp.Game;
using System.Text.Json;

namespace TapItUp;

[QueryProperty(nameof(ResultsDataJson), "resultsData")]
public partial class ResultsPage : ContentPage
{
    private GameResultsData? _resultsData;

    public string ResultsDataJson { get; set; } = "";

    public ResultsPage()
    {
        InitializeComponent();
        SizeChanged += OnPageSizeChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (!string.IsNullOrEmpty(ResultsDataJson))
        {
            LoadResults();
        }

        // Set initial layout based on current size
        UpdateLayoutForOrientation();
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        UpdateLayoutForOrientation();
    }

    private void UpdateLayoutForOrientation()
    {
        var isLandscape = Width > Height;

        PortraitResults.IsVisible = !isLandscape;
        LandscapeResults.IsVisible = isLandscape;
    }

    private void LoadResults()
    {
        try
        {
            var decodedJson = Uri.UnescapeDataString(ResultsDataJson);
            _resultsData = JsonSerializer.Deserialize<GameResultsData>(decodedJson);

            if (_resultsData != null)
            {
                DisplayResults();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Error loading results: {ex.Message}");
            DisplayAlert("Error", "Failed to load game results", "OK");
        }
    }

    private void DisplayResults()
    {
        if (_resultsData == null) return;

        // Song info with S/D notation
        SongTitleLabel.Text = _resultsData.SongTitle;
        var chartNotation = GetChartNotation(_resultsData.ChartStepType, _resultsData.ChartMeter);
        SongMetaLabel.Text = $"{_resultsData.SongArtist} - {chartNotation}";

        // Format score with commas
        var formattedScore = _resultsData.Score.ToString("N0");
        var gradeColor = PhoenixScoring.GetGradeColor(_resultsData.Grade);
        var plateColor = PhoenixScoring.GetPlateColor(_resultsData.Plate);

        // Portrait labels
        ScoreLabel.Text = formattedScore;
        GradeLabel.Text = _resultsData.Grade;
        GradeLabel.TextColor = gradeColor;
        PlateLabel.Text = _resultsData.Plate;
        PlateLabel.TextColor = plateColor;
        AccuracyLabel.Text = $"{_resultsData.Accuracy:F2}%";
        MaxComboLabel.Text = _resultsData.MaxCombo.ToString();
        PerfectLabel.Text = _resultsData.PerfectCount.ToString();
        GreatLabel.Text = _resultsData.GreatCount.ToString();
        GoodLabel.Text = _resultsData.GoodCount.ToString();
        BadLabel.Text = _resultsData.BadCount.ToString();
        MissLabel.Text = _resultsData.MissCount.ToString();

        // Landscape labels (duplicate data for landscape layout)
        ScoreLabelLandscape.Text = formattedScore;
        GradeLabelLandscape.Text = _resultsData.Grade;
        GradeLabelLandscape.TextColor = gradeColor;
        PlateLabelLandscape.Text = _resultsData.Plate;
        PlateLabelLandscape.TextColor = plateColor;
        AccuracyLabelLandscape.Text = $"{_resultsData.Accuracy:F2}%";
        MaxComboLabelLandscape.Text = _resultsData.MaxCombo.ToString();
        PerfectLabelLandscape.Text = _resultsData.PerfectCount.ToString();
        GreatLabelLandscape.Text = _resultsData.GreatCount.ToString();
        GoodLabelLandscape.Text = _resultsData.GoodCount.ToString();
        BadLabelLandscape.Text = _resultsData.BadCount.ToString();
        MissLabelLandscape.Text = _resultsData.MissCount.ToString();
    }

    /// <summary>
    /// Converts StepType to S/D notation (e.g., "pump-single" -> "S", "pump-double" -> "D")
    /// </summary>
    private static string GetChartNotation(string stepType, int meter)
    {
        var prefix = stepType?.ToLower() switch
        {
            "pump-single" => "S",
            "pump-double" => "D",
            _ => "" // Fallback for unknown step types
        };

        return string.IsNullOrEmpty(prefix) ? meter.ToString() : $"{prefix}{meter}";
    }

    private async void OnSongSelectClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SongSelectPage");
    }

    private sealed class GameResultsData
    {
        public string SongTitle { get; set; } = "";
        public string SongArtist { get; set; } = "";
        public string ChartDifficulty { get; set; } = "";
        public string ChartStepType { get; set; } = ""; // Add this new property
        public int ChartMeter { get; set; }
        public int Score { get; set; }
        public string Grade { get; set; } = "";
        public string Plate { get; set; } = "";
        public double Accuracy { get; set; }
        public int MaxCombo { get; set; }
        public int PerfectCount { get; set; }
        public int GreatCount { get; set; }
        public int GoodCount { get; set; }
        public int BadCount { get; set; }
        public int MissCount { get; set; }
    }
}