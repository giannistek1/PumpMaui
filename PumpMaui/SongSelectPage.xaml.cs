using CommunityToolkit.Maui.Storage;
using PumpMaui.Game;
using PumpMaui.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace PumpMaui;

public partial class SongSelectPage : ContentPage, INotifyPropertyChanged
{
    private SscSong? _selectedSong;
    private SscChart? _selectedChart;
    private double _scrollSpeed = GameConstants.DefaultScrollSpeed;
    private List<SscChart> _currentSortedCharts = [];

    public ObservableCollection<SongListItem> SongList { get; } = [];
    public ObservableCollection<GameSeriesItem> GameSeriesList { get; } = [];

    private Dictionary<string, List<SscSong>> _songsBySeries = [];

    private bool _hasSelection;
    public bool HasSelection
    {
        get => _hasSelection;
        private set { if (_hasSelection == value) return; _hasSelection = value; OnPropertyChanged(); }
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

    private string _noteSkin = "Prime";
    public string NoteSkin
    {
        get => _noteSkin;
        set { if (_noteSkin == value) return; _noteSkin = value; OnPropertyChanged(); }
    }

    private bool _isSeriesSelectionVisible = true;
    public bool IsSeriesSelectionVisible
    {
        get => _isSeriesSelectionVisible;
        set
        {
            if (_isSeriesSelectionVisible == value) return;
            _isSeriesSelectionVisible = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSongListVisible));
        }
    }

    public bool IsSongListVisible => !IsSeriesSelectionVisible;

    public SongSelectPage()
    {
        InitializeComponent();
        BindingContext = this;
        Routing.RegisterRoute("GamePage", typeof(GamePage));
        SizeChanged += OnPageSizeChanged;
    }

    // -------------------------------------------------------------------------
    // Layout / orientation
    // -------------------------------------------------------------------------

    private void OnPageSizeChanged(object sender, EventArgs e)
    {
        var isLandscape = Width > Height;

        PortraitLayout.IsVisible = !isLandscape;
        LandscapeLayout.IsVisible = isLandscape;

        if (isLandscape)
        {
            if (LandscapeSongListView != null)
            {
                LandscapeSongListView.SelectionChanged -= OnSongSelected;
                LandscapeSongListView.SelectedItem = SongListView.SelectedItem;
                LandscapeSongListView.SelectionChanged += OnSongSelected;
            }

            if (HasSelection && _selectedSong != null)
            {
                if (LandscapeSelectedTitleLabel != null) LandscapeSelectedTitleLabel.Text = _selectedSong.Title;
                if (LandscapeSelectedArtistLabel != null) LandscapeSelectedArtistLabel.Text = _selectedSong.Artist;

                if (LandscapeChartPicker != null)
                {
                    LandscapeChartPicker.SelectedIndexChanged -= OnChartChanged;
                    LandscapeChartPicker.Items.Clear();
                    foreach (var item in ChartPicker.Items)
                        LandscapeChartPicker.Items.Add(item);
                    LandscapeChartPicker.SelectedIndex = ChartPicker.SelectedIndex;
                    LandscapeChartPicker.SelectedIndexChanged += OnChartChanged;
                }
            }
        }
        else
        {
            if (LandscapeSongListView?.SelectedItem != null)
            {
                SongListView.SelectionChanged -= OnSongSelected;
                SongListView.SelectedItem = LandscapeSongListView.SelectedItem;
                SongListView.SelectionChanged += OnSongSelected;
            }
        }
    }

    // -------------------------------------------------------------------------
    // App-package (embedded) song loading
    // -------------------------------------------------------------------------

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (SongList.Count == 0)
            _ = LoadEmbeddedSongsAsync();
    }

    private async Task LoadEmbeddedSongsAsync()
    {
        await Task.Delay(100); // allow UI to fully attach

        try
        {
            var loadedCount = 0;

            foreach (var songPath in GameConstants.Songs)
            {
                try
                {
                    await using var stream = await FileSystem.OpenAppPackageFileAsync(songPath);
                    using var reader = new StreamReader(stream);
                    var content = await reader.ReadToEndAsync();

                    var song = SscParser.Parse(content, songPath);
                    if (song.Charts?.Count > 0)
                    {
                        AddSongToSeries(song, GetGameSeries(songPath), bannerPath: null);
                        loadedCount++;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SongSelect] Failed to load embedded song {songPath}: {ex.Message}");
                }
            }

            if (loadedCount == 0)
                await DisplayAlert("No Songs", "No embedded songs could be loaded.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load embedded songs: {ex.Message}", "OK");
        }
    }

    // -------------------------------------------------------------------------
    // External folder loading
    // -------------------------------------------------------------------------

    private async void OnOpenFolderClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FolderPicker.Default.PickAsync(CancellationToken.None);

            if (result == null || !result.IsSuccessful) return;

            var folderPath = result.Folder.Path;
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                await DisplayAlert("Error", "Could not read the selected folder path.", "OK");
                return;
            }

#if ANDROID
            var safUri = folderPath.StartsWith("content://", StringComparison.OrdinalIgnoreCase)
                ? folderPath
                : ConvertPathToSafUri(folderPath);

            await LoadSongsFromSafAsync(safUri);
#else
            await LoadSongsFromFileSystemAsync(folderPath);
#endif
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to open folder picker: {ex.Message}", "OK");
        }
    }

#if ANDROID
    /// <summary>
    /// Converts a plain Android filesystem path (e.g. /storage/emulated/0/Songs or
    /// /storage/XXXX-XXXX/Songs for SD card) to a SAF content:// tree URI.
    /// </summary>
    private static string ConvertPathToSafUri(string path)
    {
        path = path.Replace('\\', '/').TrimEnd('/');
        const string authority = "com.android.externalstorage.documents";

        if (path.StartsWith("/storage/emulated/0", StringComparison.OrdinalIgnoreCase))
        {
            var relative = path["/storage/emulated/0".Length..].TrimStart('/');
            var docId = string.IsNullOrEmpty(relative) ? "primary:" : $"primary:{relative}";
            return $"content://{authority}/tree/{Uri.EscapeDataString(docId)}";
        }

        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 && parts[0].Equals("storage", StringComparison.OrdinalIgnoreCase))
        {
            var volumeId = parts[1];
            var relative = parts.Length > 2 ? string.Join("/", parts.Skip(2)) : string.Empty;
            var docId = string.IsNullOrEmpty(relative) ? $"{volumeId}:" : $"{volumeId}:{relative}";
            return $"content://{authority}/tree/{Uri.EscapeDataString(docId)}";
        }

        return path; // fallback — scanner will log if it still fails
    }

    private async Task LoadSongsFromSafAsync(string treeUriString)
    {
        var context = Android.App.Application.Context;
        var loadedCount = 0;
        var errorCount = 0;

        List<PumpMaui.Platforms.Android.ScanResult> scanResults;
        try
        {
            scanResults = await Task.Run(() =>
                PumpMaui.Platforms.Android.AndroidSafScanner.Scan(context, treeUriString));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to scan folder: {ex.Message}", "OK");
            return;
        }

        if (scanResults.Count == 0)
        {
            await DisplayAlert("No Songs Found",
                "No .ssc files were found.\n\nExpected structure:\n  Root / Game Series / Song / song.ssc",
                "OK");
            return;
        }

        foreach (var result in scanResults)
        {
            try
            {
                var content = await PumpMaui.Platforms.Android.AndroidSafScanner.ReadTextAsync(context, result.SscUri);
                var song = SscParser.Parse(content, result.SscUri);
                song.SongDocumentUri = result.SongDocumentUri;

                if (song.Charts.Count == 0) continue;

                if (!_songsBySeries.ContainsKey(result.SeriesName))
                {
                    _songsBySeries[result.SeriesName] = [];

                    ImageSource? bannerImage = null;
                    if (!string.IsNullOrEmpty(result.BannerUri))
                    {
                        var capturedUri = result.BannerUri;
                        bannerImage = ImageSource.FromStream(
                            () => PumpMaui.Platforms.Android.AndroidSafScanner.OpenRead(context, capturedUri));
                    }

                    GameSeriesList.Add(new GameSeriesItem { Name = result.SeriesName, Banner = bannerImage });
                }

                _songsBySeries[result.SeriesName].Add(song);
                loadedCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                System.Diagnostics.Debug.WriteLine($"[SongSelect] SAF load failed for {result.SscUri}: {ex.Message}");
            }
        }

        var message = loadedCount == 0
            ? "No songs with valid charts were found.\n\nExpected structure:\n  Root / Game Series / Song / song.ssc"
            : $"Loaded {loadedCount} song(s) across {_songsBySeries.Count} series.";

        if (errorCount > 0)
            message += $"\n({errorCount} file(s) failed to parse.)";

        await DisplayAlert(loadedCount == 0 ? "No Songs Found" : "Songs Loaded", message, "OK");
    }
#endif

    private async Task LoadSongsFromFileSystemAsync(string rootPath)
    {
        var loadedCount = 0;
        var errorCount = 0;

        try
        {
            if (!Directory.Exists(rootPath))
            {
                await DisplayAlert("Folder Not Found", $"The path could not be accessed:\n{rootPath}", "OK");
                return;
            }

            foreach (var seriesDir in Directory.GetDirectories(rootPath))
            {
                var seriesName = Path.GetFileName(seriesDir).ToUpperInvariant();

                foreach (var songDir in Directory.GetDirectories(seriesDir))
                {
                    var sscFiles = Directory.GetFiles(songDir, "*.ssc");
                    if (sscFiles.Length == 0) continue;

                    try
                    {
                        var content = await File.ReadAllTextAsync(sscFiles[0]);
                        var song = SscParser.Parse(content, sscFiles[0]);
                        if (song.Charts.Count == 0) continue;

                        var bannerPath = new[] {
                            Path.Combine(seriesDir, "banner.png"),
                            Path.Combine(seriesDir, "banner.jpg"),
                        }.FirstOrDefault(File.Exists);

                        AddSongToSeries(song, seriesName, bannerPath);
                        loadedCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        System.Diagnostics.Debug.WriteLine($"[SongSelect] Failed to load {sscFiles[0]}: {ex.Message}");
                    }
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            await DisplayAlert("Permission Denied", $"Cannot read:\n{rootPath}", "OK");
            return;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to scan folder: {ex.Message}", "OK");
            return;
        }

        var message = loadedCount == 0
            ? "No songs with valid charts were found.\n\nExpected structure:\n  Root / Game Series / Song / song.ssc"
            : $"Loaded {loadedCount} song(s) across {_songsBySeries.Count} series.";

        if (errorCount > 0)
            message += $"\n({errorCount} file(s) failed to parse.)";

        await DisplayAlert(loadedCount == 0 ? "No Songs Found" : "Songs Loaded", message, "OK");
    }

    // -------------------------------------------------------------------------
    // Remote URL loading
    // -------------------------------------------------------------------------

    private async void OnLoadRemoteClicked(object sender, EventArgs e)
    {
        var url = await DisplayPromptAsync(
            "Remote Song Library",
            "Enter your CDN URL:",
            placeholder: "https://pub-fac3ff2c2b384776b2761efc75069033.r2.dev",
            initialValue: Preferences.Get("RemoteBaseUrl", "https://pub-fac3ff2c2b384776b2761efc75069033.r2.dev"));

        if (string.IsNullOrWhiteSpace(url)) return;

        Preferences.Set("RemoteBaseUrl", url.TrimEnd('/'));

        SetLoadingVisible(true);

        try
        {
            LoadRemoteButtonPortrait.IsEnabled = false;
            LoadRemoteButtonLandscape.IsEnabled = false;
            LoadRemoteButtonPortrait.Text = "Loading...";
            LoadRemoteButtonLandscape.Text = "Loading...";

            var progress = new Progress<LoadProgress>(p => MainThread.BeginInvokeOnMainThread(() =>
            {
                var text = $"{p.Message} ({p.Current}/{p.Total})";
                LoadingLabelPortrait.Text = text;
                LoadingProgressBarPortrait.Progress = p.Percentage;
                LoadingLabelLandscape.Text = text;
                LoadingProgressBarLandscape.Progress = p.Percentage;
            }));

            var songs = await RemoteSongService.LoadSongsAsync(url, progress);

            foreach (var song in songs.Where(s => s.Charts.Count > 0))
            {
                var series = GetGameSeriesFromUrl(song.SourcePath ?? string.Empty);
                if (!_songsBySeries.ContainsKey(series))
                {
                    _songsBySeries[series] = [];
                    GameSeriesList.Add(new GameSeriesItem
                    {
                        Name = series,
                        Banner = ImageSource.FromStream(() =>
                            FileSystem.OpenAppPackageFileAsync($"banner_{series.Replace(" ", "").ToLower()}.png")
                                      .GetAwaiter().GetResult())
                    });
                }
                _songsBySeries[series].Add(song);
            }

            await DisplayAlert(songs.Count == 0 ? "No Songs" : "Done",
                songs.Count == 0 ? "No songs found at that URL." : $"Loaded {songs.Count} remote song(s).",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not load remote songs:\n{ex.Message}", "OK");
        }
        finally
        {
            LoadRemoteButtonPortrait.IsEnabled = true;
            LoadRemoteButtonPortrait.Text = "Load from URL";
            LoadRemoteButtonLandscape.IsEnabled = true;
            LoadRemoteButtonLandscape.Text = "Load from URL";
            SetLoadingVisible(false);
        }
    }

    private void SetLoadingVisible(bool visible)
    {
        LoadingLabelPortrait.IsVisible = visible;
        LoadingProgressBarPortrait.IsVisible = visible;
        LoadingProgressBarPortrait.Progress = 0;
        LoadingLabelLandscape.IsVisible = visible;
        LoadingProgressBarLandscape.IsVisible = visible;
        LoadingProgressBarLandscape.Progress = 0;
    }

    // -------------------------------------------------------------------------
    // Song / chart list management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Adds a song to the internal series dictionary and <see cref="GameSeriesList" />,
    /// creating the series entry if it doesn't exist yet.
    /// </summary>
    private void AddSongToSeries(SscSong song, string seriesName, string? bannerPath)
    {
        if (!_songsBySeries.ContainsKey(seriesName))
        {
            _songsBySeries[seriesName] = [];

            ImageSource? bannerImage = null;

            if (bannerPath != null)
            {
                // External file — use stream so it works on Android
                var captured = bannerPath;
                bannerImage = ImageSource.FromStream(() => File.OpenRead(captured));
            }
            else
            {
                // Try loading an embedded banner from the app package
                var embeddedBannerPath = $"banner_{seriesName.Replace(" ", "").ToLower()}.png";
                try
                {
                    var ms = new MemoryStream();
                    using var s = FileSystem.OpenAppPackageFileAsync(embeddedBannerPath).GetAwaiter().GetResult();
                    s.CopyTo(ms);
                    ms.Position = 0;
                    bannerImage = ImageSource.FromStream(() => ms);
                }
                catch { /* no banner — that's fine */ }
            }

            GameSeriesList.Add(new GameSeriesItem { Name = seriesName, Banner = bannerImage });
        }

        _songsBySeries[seriesName].Add(song);
    }

    private void AddSong(SscSong song)
    {
        SongList.Add(new SongListItem
        {
            Song = song,
            Title = song.Title,
            Artist = song.Artist,
            ChartSummary = GenerateChartSummary(song),
            BackgroundImageSource = TryCreateBackground(song)
        });
    }

    private static ImageSource? TryCreateBackground(SscSong song)
    {
        if (string.IsNullOrWhiteSpace(song.SourcePath) || string.IsNullOrWhiteSpace(song.BackgroundPath))
            return null;

        try
        {
            var baseDir = Path.GetDirectoryName(song.SourcePath);
            if (string.IsNullOrWhiteSpace(baseDir)) return null;

            if (!Path.IsPathRooted(song.SourcePath))
            {
                // Embedded app-package asset
                var relativePath = Path.Combine(baseDir, song.BackgroundPath.Replace('\\', '/'))
                                       .Replace('\\', '/');
                return ImageSource.FromStream(() =>
                    FileSystem.OpenAppPackageFileAsync(relativePath).GetAwaiter().GetResult());
            }
            else
            {
                // External file
                var absolutePath = Path.Combine(baseDir,
                    song.BackgroundPath.Replace('/', Path.DirectorySeparatorChar));

                if (!File.Exists(absolutePath)) return null;
                return ImageSource.FromStream(() => File.OpenRead(absolutePath));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SongSelect] Background load failed: {ex.Message}");
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Series / song selection
    // -------------------------------------------------------------------------

    private void OnSeriesSelected(object sender, SelectionChangedEventArgs e)
    {
        var selected = e.CurrentSelection.FirstOrDefault() as GameSeriesItem;
        if (selected == null) return;

        SongList.Clear();

        if (_songsBySeries.TryGetValue(selected.Name, out var songs))
            foreach (var song in songs)
                AddSong(song);

        IsSeriesSelectionVisible = false;
    }

    private void OnBackToSeriesClicked(object sender, EventArgs e)
    {
        SongList.Clear();
        _selectedSong = null;
        _selectedChart = null;
        HasSelection = false;

        PortraitSeriesCollectionView.SelectedItem = null;
        LandscapeSeriesCollectionView.SelectedItem = null;

        IsSeriesSelectionVisible = true;
    }

    private void OnSongSelected(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = e.CurrentSelection.FirstOrDefault() as SongListItem;

        if (selectedItem == null)
        {
            _selectedSong = null;
            _selectedChart = null;
            HasSelection = false;
            return;
        }

        // Tap the same song again to deselect
        if (_selectedSong == selectedItem.Song)
        {
            _selectedSong = null;
            _selectedChart = null;
            HasSelection = false;

            SongListView.SelectionChanged -= OnSongSelected;
            SongListView.SelectedItem = null;
            SongListView.SelectionChanged += OnSongSelected;

            if (LandscapeSongListView != null)
            {
                LandscapeSongListView.SelectionChanged -= OnSongSelected;
                LandscapeSongListView.SelectedItem = null;
                LandscapeSongListView.SelectionChanged += OnSongSelected;
            }

            return;
        }

        _selectedSong = selectedItem.Song;
        HasSelection = true;

        SelectedTitleLabel.Text = _selectedSong.Title;
        SelectedArtistLabel.Text = _selectedSong.Artist;

        if (LandscapeSelectedTitleLabel != null) LandscapeSelectedTitleLabel.Text = _selectedSong.Title;
        if (LandscapeSelectedArtistLabel != null) LandscapeSelectedArtistLabel.Text = _selectedSong.Artist;

        // Sync the other CollectionView without re-firing the event
        if (sender == SongListView && LandscapeSongListView != null)
        {
            LandscapeSongListView.SelectionChanged -= OnSongSelected;
            LandscapeSongListView.SelectedItem = selectedItem;
            LandscapeSongListView.SelectionChanged += OnSongSelected;
        }
        else if (sender == LandscapeSongListView)
        {
            SongListView.SelectionChanged -= OnSongSelected;
            SongListView.SelectedItem = selectedItem;
            SongListView.SelectionChanged += OnSongSelected;
        }

        PopulateCharts(_selectedSong);
    }

    private void OnCloseSelectionClicked(object sender, EventArgs e)
    {
        SongListView.SelectedItem = null;
        LandscapeSongListView.SelectedItem = null;
        _selectedSong = null;
        _selectedChart = null;
        HasSelection = false;

        ChartPicker.Items.Clear();
        LandscapeChartPicker.Items.Clear();
        _currentSortedCharts.Clear();
        SelectedChartBorder.IsVisible = false;

        SelectedTitleLabel.Text = "";
        SelectedArtistLabel.Text = "";
        LandscapeSelectedTitleLabel.Text = "";
        LandscapeSelectedArtistLabel.Text = "";
    }

    // -------------------------------------------------------------------------
    // Chart selection
    // -------------------------------------------------------------------------

    private void PopulateCharts(SscSong? song)
    {
        ChartPicker.Items.Clear();
        LandscapeChartPicker?.Items.Clear();
        _currentSortedCharts.Clear();
        SelectedChartBorder.IsVisible = false;

        if (song?.Charts == null || song.Charts.Count == 0) return;

        _currentSortedCharts = song.Charts
            .OrderBy(c => c.StepType?.ToLower() == "pump-single" ? 0 : 1)
            .ThenBy(c => c.Meter)
            .ToList();

        foreach (var chart in _currentSortedCharts)
        {
            var text = GetChartDisplayText(chart);
            ChartPicker.Items.Add(text);
            LandscapeChartPicker?.Items.Add(text);
        }

        if (ChartPicker.Items.Count > 0)
        {
            ChartPicker.SelectedIndex = 0;
            if (LandscapeChartPicker != null)
                LandscapeChartPicker.SelectedIndex = 0;

            _selectedChart = _currentSortedCharts.First();
            UpdateSelectedChartDisplay();
        }
    }

    private void OnChartChanged(object sender, EventArgs e)
    {
        var picker = sender as Picker;
        var selectedIndex = picker?.SelectedIndex ?? -1;

        if (_selectedSong == null || selectedIndex < 0 || selectedIndex >= _currentSortedCharts.Count)
        {
            SelectedChartBorder.IsVisible = false;
            return;
        }

        if (picker == ChartPicker && LandscapeChartPicker != null)
            LandscapeChartPicker.SelectedIndex = selectedIndex;
        else if (picker == LandscapeChartPicker)
            ChartPicker.SelectedIndex = selectedIndex;

        _selectedChart = _currentSortedCharts[selectedIndex];
        UpdateSelectedChartDisplay();
    }

    private void UpdateSelectedChartDisplay()
    {
        if (_selectedChart == null) { SelectedChartBorder.IsVisible = false; return; }

        SelectedChartLevelLabel.Text = GetChartDisplayText(_selectedChart);
        SelectedChartNotesLabel.Text = $"{_selectedChart.Notes.Count} notes";
        SelectedChartBorder.IsVisible = true;
    }

    // -------------------------------------------------------------------------
    // Play
    // -------------------------------------------------------------------------

    private async void OnPlayClicked(object sender, EventArgs e)
    {
        if (_selectedSong == null || _selectedChart == null)
        {
            await DisplayAlert("No Selection", "Please select a song and chart first.", "OK");
            return;
        }

        try
        {
            var audioUrl = !string.IsNullOrWhiteSpace(_selectedSong.BaseUrl)
                ? new Uri(RemoteSongService.ResolveAssetUrl(_selectedSong, _selectedSong.MusicPath)).AbsoluteUri
                : null;

            var gameData = new GameStartData
            {
                Song = _selectedSong,
                Chart = _selectedChart,
                ScrollSpeed = ScrollSpeed,
                NoteSkin = NoteSkin,
                RemoteAudioUrl = audioUrl
            };

            var encodedJson = Uri.EscapeDataString(JsonSerializer.Serialize(gameData));
            await Shell.Current.GoToAsync("GamePage", new Dictionary<string, object> { { "songData", encodedJson } });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to start game: {ex.Message}", "OK");
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string GetChartDisplayText(SscChart chart)
    {
        var prefix = chart.StepType?.ToLower() switch
        {
            "pump-single" => "S",
            "pump-double" => "D",
            _ => ""
        };

        if (!string.IsNullOrEmpty(prefix)) return $"{prefix}{chart.Meter}";

        return string.IsNullOrEmpty(chart.Difficulty)
            ? $"Level {chart.Meter}"
            : chart.Meter > 0 ? $"{chart.Difficulty} {chart.Meter}" : chart.Difficulty;
    }

    private static string GenerateChartSummary(SscSong song)
    {
        if (song.Charts.Count == 0) return "No charts available";

        var parts = new List<string>();

        var singles = song.Charts.Where(c => c.StepType?.ToLower() == "pump-single" && c.Meter > 0)
                          .Select(c => c.Meter).Distinct().OrderBy(x => x).ToList();
        var doubles = song.Charts.Where(c => c.StepType?.ToLower() == "pump-double" && c.Meter > 0)
                          .Select(c => c.Meter).Distinct().OrderBy(x => x).ToList();

        if (singles.Count > 0) parts.Add(singles.First() == singles.Last() ? $"S{singles.First()}" : $"S{singles.First()}-{singles.Last()}");
        if (doubles.Count > 0) parts.Add(doubles.First() == doubles.Last() ? $"D{doubles.First()}" : $"D{doubles.First()}-{doubles.Last()}");

        return parts.Count == 0
            ? $"{song.Charts.Count} chart(s)"
            : $"{string.Join(", ", parts)} • {song.Charts.Count} chart(s)";
    }

    private static string GetGameSeries(string path)
    {
        path = path.Replace('\\', '/');
        var part = path.Split('/', StringSplitOptions.RemoveEmptyEntries)
                       .FirstOrDefault(p => p.Contains(" - "));
        if (string.IsNullOrEmpty(part)) return "UNKNOWN";
        var split = part.Split('-', 2);
        return split.Length < 2 ? "UNKNOWN" : split[1].Trim().ToUpperInvariant();
    }

    private static string GetGameSeriesFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "UNKNOWN";
        var path = Uri.UnescapeDataString(new Uri(url).AbsolutePath);
        var part = path.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrEmpty(part)) return "UNKNOWN";
        var split = part.Split('-', 2);
        return split.Length < 2 ? "UNKNOWN" : split[1].Trim().ToUpperInvariant();
    }

    private void OnScrollSpeedChanged(object sender, ValueChangedEventArgs e) => ScrollSpeed = e.NewValue;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class SongListItem
{
    public required SscSong Song { get; set; }
    public required string Title { get; set; }
    public required string Artist { get; set; }
    public required string ChartSummary { get; set; }
    public ImageSource? BackgroundImageSource { get; set; }
}

public class GameStartData
{
    public required SscSong Song { get; set; }
    public required SscChart Chart { get; set; }
    public double ScrollSpeed { get; set; } = GameConstants.DefaultScrollSpeed;
    public string NoteSkin { get; set; } = "Prime";
    public string? RemoteAudioUrl { get; set; }
}