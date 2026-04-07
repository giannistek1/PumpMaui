using Microsoft.Maui.Graphics;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace TapItUp.Game;

public sealed class NoteFieldDrawable : IDrawable
{
    private static readonly Color[] LaneColors = [
        Color.FromArgb("#00C2FF"), // Blue (bottom left)
        Color.FromArgb("#FF2D2D"), // Red (top left)
        Color.FromArgb("#FFE45E"), // Yellow (center)
        Color.FromArgb("#FF2D2D"), // Red (top right)
        Color.FromArgb("#00C2FF")  // Blue (bottom right)
    ];
    private readonly RhythmGameEngine _engine;

    // Image cache for different skins
    private static IImage? _centerPrime, _downleftPrime, _upleftPrime;
    private static IImage? _centerFiestaEx, _downleftFiestaEx, _upleftFiestaEx;
    private static IImage? _centerNxa, _downleftNxa, _upleftNxa;
    private static IImage? _centerOld, _downleftOld, _upleftOld;

    // Grayscale receptor images (center + others) for skins
    private static IImage? _grayPrime, _graycPrime;
    private static IImage? _grayFiestaEx, _graycFiestaEx;
    private static IImage? _grayNxa, _graycNxa;
    private static IImage? _grayOld, _graycOld;

    // Fixed race condition: use Task to ensure proper initialization
    private static Task? _loadingTask;
    private static bool _imagesLoaded = false;

    // Debug flag for note borders (default: false = no borders)
    public bool ShowNoteBorders { get; set; } = false;

    // Note skin property
    public string NoteSkin { get; set; } = "Prime";

    /// <summary>
    /// Arrow Velocity (300–999). The visible scroll window is 720 / Av seconds,
    /// matching real Pump It Up scroll timing (AV 180 ≈ 4 s, AV 270 ≈ 2.67 s, AV 470 ≈ 1.53 s).
    /// </summary>
    public int Av { get; set; } = GameConstants.DefaultAv;

    public NoteFieldDrawable(RhythmGameEngine engine)
    {
        _engine = engine;
        // Fix race condition: ensure loading task starts only once
        if (_loadingTask == null)
        {
            _loadingTask = LoadImagesAsync();
        }
    }

    public double ScrollSpeedMultiplier { get; set; } = GameConstants.DefaultAv / 150d; // default AV 300 at 150 BPM
    public bool IsLandscapeMode { get; set; } = false;

    private static async Task LoadImagesAsync()
    {
        if (_imagesLoaded) return;

        try
        {
            System.Diagnostics.Debug.WriteLine("🖼️ Starting image loading for all note skins...");

            // Load Prime skin images
            _centerPrime = await LoadMauiAsset("center_prime.png");
            _downleftPrime = await LoadMauiAsset("downleft_prime.png");
            _upleftPrime = await LoadMauiAsset("upleft_prime.png");

            // Load FiestaEx skin images
            _centerFiestaEx = await LoadMauiAsset("center_fiestaex.png");
            _downleftFiestaEx = await LoadMauiAsset("downleft_fiestaex.png");
            _upleftFiestaEx = await LoadMauiAsset("upleft_fiestaex.png");

            // Load NXA skin images
            _centerNxa = await LoadMauiAsset("center_nxa.png");
            _downleftNxa = await LoadMauiAsset("downleft_nxa.png");
            _upleftNxa = await LoadMauiAsset("upleft_nxa.png");

            // Load old skin images
            _centerOld = await LoadMauiAsset("center_old.png");
            _downleftOld = await LoadMauiAsset("downleft_old.png");
            _upleftOld = await LoadMauiAsset("upleft_old.png");

            // Load grayscale receptor images (prime + variants)
            _grayPrime = await LoadMauiAsset("gray_prime.png");
            _graycPrime = await LoadMauiAsset("grayc_prime.png");

            _grayFiestaEx = await LoadMauiAsset("gray_fiestaex.png");
            _graycFiestaEx = await LoadMauiAsset("grayc_fiestaex.png");

            _grayNxa = await LoadMauiAsset("gray_nxa.png");
            _graycNxa = await LoadMauiAsset("grayc_nxa.png");

            _grayOld = await LoadMauiAsset("gray_old.png");
            _graycOld = await LoadMauiAsset("grayc_old.png");

            System.Diagnostics.Debug.WriteLine($"🖼️ Image loading summary - Prime: Center={_centerPrime != null}, Blue={_downleftPrime != null}, Red={_upleftPrime != null}");
            System.Diagnostics.Debug.WriteLine($"🖼️ Image loading summary - Fiestaex: Center={_centerFiestaEx != null}, Blue={_downleftFiestaEx != null}, Red={_upleftFiestaEx != null}");
            System.Diagnostics.Debug.WriteLine($"🖼️ Image loading summary - NXA: Center={_centerNxa != null}, Blue={_downleftNxa != null}, Red={_upleftNxa != null}");
            System.Diagnostics.Debug.WriteLine($"🖼️ Image loading summary - Old: Center={_centerOld != null}, Blue={_downleftOld != null}, Red={_upleftOld != null}");
            System.Diagnostics.Debug.WriteLine($"🖼️ Gray images - prime={_grayPrime != null}, grayc={_graycPrime != null}, fiestaex={_grayFiestaEx != null}, grayc_fiestaex={_graycFiestaEx != null}");

            _imagesLoaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to load note images: {ex.Message}");
            _imagesLoaded = true;
        }
    }

    // Helper method to get the correct images based on selected skin
    private IImage? GetCenterImage()
    {
        return NoteSkin.ToLower() switch
        {
            "fiestaex" => _centerFiestaEx,
            "old" => _centerOld,
            "nxa" => _centerNxa,
            _ => _centerPrime // Default to Prime
        };
    }

    private IImage? GetBlueArrowImage()
    {
        return NoteSkin.ToLower() switch
        {
            "fiestaex" => _downleftFiestaEx,
            "old" => _downleftOld,
            "nxa" => _downleftNxa,
            _ => _downleftPrime // Default to Prime
        };
    }

    private IImage? GetRedArrowImage()
    {
        return NoteSkin.ToLower() switch
        {
            "fiestaex" => _upleftFiestaEx,
            "old" => _upleftOld,
            "nxa" => _upleftNxa,
            _ => _upleftPrime // Default to Prime
        };
    }

    // New helpers for grayscale receptor images
    private IImage? GetGrayImage()
    {
        return NoteSkin.ToLower() switch
        {
            "fiestaex" => _grayFiestaEx,
            "old" => _grayOld,
            "nxa" => _grayNxa,
            _ => _grayPrime
        };
    }

    private IImage? GetGrayCenterImage()
    {
        return NoteSkin.ToLower() switch
        {
            "fiestaex" => _graycFiestaEx,
            "old" => _graycOld,
            "nxa" => _graycNxa,
            _ => _graycPrime
        };
    }

    private static async Task<IImage?> LoadMauiAsset(string filename)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"🔍 Attempting to load: {filename}");

            Stream? stream = null;
            string? successfulName = null;

            // Generate all possible filename variations that MAUI might create
            var filenameWithoutExtension = Path.GetFileNameWithoutExtension(filename);
            var extension = Path.GetExtension(filename);

            var possibleNames = new List<string>
            {
                filename,                                           // yellow_center.png
                filename.Replace("_", "-"),                        // yellow-center.png
                $"{filenameWithoutExtension}.scale-100{extension}", // yellow_center.scale-100.png
                $"{filenameWithoutExtension.Replace("_", "-")}.scale-100{extension}", // yellow-center.scale-100.png
                $"{filenameWithoutExtension}.scale-200{extension}", // yellow_center.scale-200.png
                $"{filenameWithoutExtension}.scale-150{extension}", // yellow_center.scale-150.png
                $"{filenameWithoutExtension}.scale-125{extension}", // yellow_center.scale-125.png
            };

            // Method 1: Try FileSystem.OpenAppPackageFileAsync with all possible names
            foreach (var name in possibleNames)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine($"  📦 Trying FileSystem.OpenAppPackageFileAsync({name})");
                    stream = await FileSystem.OpenAppPackageFileAsync(name);
                    successfulName = name;
                    System.Diagnostics.Debug.WriteLine($"  ✅ Success with FileSystem.OpenAppPackageFileAsync: {name}");
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"  ❌ Failed {name}: {ex.Message}");
                }
            }

            // Method 2: If FileSystem approach fails, try embedded resources
            if (stream == null)
            {
                try
                {
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                    // Try multiple resource name formats
                    var resourceNames = new List<string>();
                    foreach (var name in possibleNames)
                    {
                        resourceNames.Add($"TapItUp.Resources.Raw.{name}");
                        resourceNames.Add($"Resources.Raw.{name}");
                        resourceNames.Add($"Raw.{name}");
                        resourceNames.Add(name);
                    }

                    foreach (var resourceName in resourceNames)
                    {
                        System.Diagnostics.Debug.WriteLine($"  📦 Trying embedded resource: {resourceName}");
                        stream = assembly.GetManifestResourceStream(resourceName);

                        if (stream != null)
                        {
                            successfulName = resourceName;
                            System.Diagnostics.Debug.WriteLine($"  ✅ Success with embedded resource: {resourceName}");
                            break;
                        }
                    }

                    // If still no stream, list all available resources for debugging
                    if (stream == null)
                    {
                        System.Diagnostics.Debug.WriteLine("  📋 Available embedded resources:");
                        var allResourceNames = assembly.GetManifestResourceNames();
                        foreach (var name in allResourceNames)
                        {
                            if (name.Contains("Image") || name.Contains(".png") || name.Contains(".jpg") || name.Contains("gray") || name.Contains("yellow") || name.Contains("blue") || name.Contains("red"))
                            {
                                System.Diagnostics.Debug.WriteLine($"    - {name}");
                            }
                        }
                    }
                }
                catch (Exception ex2)
                {
                    System.Diagnostics.Debug.WriteLine($"  ❌ Embedded resource approach failed: {ex2.Message}");
                }
            }

            if (stream != null)
            {
                System.Diagnostics.Debug.WriteLine($"  🎯 Stream obtained from {successfulName}");

                using (stream)
                {
                    // Fix Android stream.Length issue: use MemoryStream + CopyToAsync
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    var bytes = ms.ToArray();

                    System.Diagnostics.Debug.WriteLine($"  📏 Copied {bytes.Length} bytes to memory stream");

                    var imageLoadingService = GetImageLoadingService();
                    if (imageLoadingService != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"  🖼️ Creating image from bytes using {imageLoadingService.GetType().Name}");
                        var image = imageLoadingService.FromBytes(bytes);
                        System.Diagnostics.Debug.WriteLine($"  ✅ Image created successfully: {image != null}");
                        return image;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"  ❌ No image loading service available");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"  ❌ No stream could be obtained for {filename} or any of its variants");
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ Failed to load image {filename}: {ex.Message}");
            return null;
        }
    }

    private static IImageLoadingService? GetImageLoadingService()
    {
        try
        {
            // Try to get the service from the application's service provider
            var serviceProvider = Application.Current?.Handler?.MauiContext?.Services;
            var service = serviceProvider?.GetService(typeof(IImageLoadingService)) as IImageLoadingService;

            if (service != null)
            {
                System.Diagnostics.Debug.WriteLine($"  🔧 Got IImageLoadingService from DI: {service.GetType().Name}");
                return service;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"  ❌ Failed to get service from DI: {ex.Message}");
        }

        // If service lookup fails, try platform-specific implementations
        try
        {
            System.Diagnostics.Debug.WriteLine($"  🔧 Trying platform-specific image loading service");
#if WINDOWS
            var platformService = new Microsoft.Maui.Graphics.Win2D.W2DImageLoadingService();
            System.Diagnostics.Debug.WriteLine($"  ✅ Created W2DImageLoadingService");
            return platformService;
#elif ANDROID
            var platformService = new Microsoft.Maui.Graphics.Platform.PlatformImageLoadingService();
            System.Diagnostics.Debug.WriteLine($"  ✅ Created Android PlatformImageLoadingService");
            return platformService;
#elif IOS || MACCATALYST
            var platformService = new Microsoft.Maui.Graphics.Platform.PlatformImageLoadingService();
            System.Diagnostics.Debug.WriteLine($"  ✅ Created iOS PlatformImageLoadingService");
            return platformService;
#else
            System.Diagnostics.Debug.WriteLine($"  ❌ No platform-specific service available");
            return null;
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"  ❌ Failed to create platform-specific service: {ex.Message}");
            return null;
        }
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        canvas.FillColor = Color.FromArgb("#090212");
        canvas.FillRectangle(dirtyRect);

        var isDouble = _engine.IsDoubleChart;
        var laneCount = isDouble ? 10 : 5;

        var topMargin = IsLandscapeMode ? 8f : 24f;
        var bottomMargin = IsLandscapeMode ? 12f : 26f;
        var receptorY = IsLandscapeMode ? 50f : 92f;
        var laneGap = 0f;

        float unit = dirtyRect.Width / laneCount;
        float[] actualWidths = Enumerable.Repeat(unit, laneCount).ToArray();

        var fieldBottom = dirtyRect.Height - bottomMargin;

        // Scroll window in seconds: 720 / AV, scaled by any in-song #SPEEDS modifier.
        var songSpeedMultiplier = GetActiveSongSpeedMultiplier();
        var scrollWindowSeconds = 720.0 / Av / songSpeedMultiplier;

        DrawLaneBackgrounds(canvas, dirtyRect, actualWidths, laneGap, receptorY, fieldBottom, laneCount);
        DrawHoldBodies(canvas, actualWidths, laneGap, receptorY, fieldBottom, laneCount, scrollWindowSeconds);
        DrawReceptors(canvas, actualWidths, laneGap, receptorY, laneCount);
        DrawNotes(canvas, actualWidths, laneGap, receptorY, fieldBottom, laneCount, scrollWindowSeconds);
        DrawFrame(canvas, dirtyRect, receptorY, topMargin);
        canvas.RestoreState();
    }

    /// <summary>
    /// Returns the #SPEEDS multiplier active at the current engine time.
    /// Falls back to 1.0 when no #SPEEDS tag is present.
    /// </summary>
    private double GetActiveSongSpeedMultiplier()
    {
        // #SPEEDS is a per-chart tag in .ssc files — read from the loaded chart.
        var speedChanges = _engine.Chart?.SpeedChanges;
        if (speedChanges == null || speedChanges.Count == 0)
            return 1.0;

        var bpmChanges = _engine.Song?.BpmChanges;
        if (bpmChanges == null || bpmChanges.Count == 0)
            return 1.0;

        var currentBeat = SecondsToBeat(_engine.CurrentTimeSeconds, bpmChanges);

        var active = speedChanges[0].Multiplier;
        foreach (var sc in speedChanges)
        {
            if (sc.Beat <= currentBeat + 0.0001)
                active = sc.Multiplier;
            else
                break;
        }

        System.Diagnostics.Debug.WriteLine($"🎵 #SPEEDS active multiplier: {active:F4} at beat {currentBeat:F2}");
        return active;
    }

    /// <summary>
    /// Converts an elapsed-time value (seconds) back to a beat number using
    /// the song's BPM change list. Inverse of BeatToSeconds in SscParser.
    /// </summary>
    private static double SecondsToBeat(double seconds, IReadOnlyList<BpmChange> bpmChanges)
    {
        if (seconds <= 0d) return 0d;

        var ordered = bpmChanges.OrderBy(c => c.Beat).ToList();
        if (ordered.Count == 0) return seconds / 60d * 120d;

        var beat = 0d;
        var currentBpm = ordered[0].Bpm;
        var lastBeat = 0d;
        var elapsed = 0d;

        foreach (var change in ordered)
        {
            var beatsInSegment = change.Beat - lastBeat;
            var secondsInSegment = beatsInSegment / currentBpm * 60d;

            if (elapsed + secondsInSegment >= seconds)
                break;

            elapsed += secondsInSegment;
            beat = change.Beat;
            lastBeat = change.Beat;
            currentBpm = change.Bpm;
        }

        beat += (seconds - elapsed) / 60d * currentBpm;
        return beat;
    }

    private void DrawLaneBackgrounds(ICanvas canvas, RectF dirtyRect, float[] actualWidths, float laneGap, float receptorY, float fieldBottom, int laneCount)
    {
        float x = laneGap;
        for (var lane = 0; lane < laneCount; lane++)
        {
            float width = actualWidths[lane];

            canvas.FillColor = Color.FromArgb("#0B0710");
            canvas.FillRoundedRectangle(x, 18f, width, fieldBottom - 6f, 18f);

            canvas.FillColor = Color.FromArgb("#0B0710").WithAlpha(0.04f);
            canvas.FillRoundedRectangle(x, receptorY - 16f, width, fieldBottom - receptorY + 18f, 18f);

            x += width + laneGap;
        }

        // Subtle separator line across field (kept neutral and very faint)
        canvas.StrokeColor = Colors.White.WithAlpha(0.06f);
        canvas.StrokeSize = 1f;
        canvas.DrawLine(0f, receptorY + 22f, dirtyRect.Width, receptorY + 22f);
    }

    private void DrawHoldBodies(ICanvas canvas, float[] actualWidths, float laneGap, float receptorY, float fieldBottom, int laneCount, double scrollWindowSeconds)
    {
        float x = laneGap;
        var travelHeight = fieldBottom - receptorY - 18f;

        for (var lane = 0; lane < laneCount; lane++)
        {
            float width = actualWidths[lane];
            var centerX = x + width / 2f;
            var laneColorIndex = lane % 5;

            var holdStarts = _engine.Notes.Where(n => n.Lane == lane && n.Type == NoteType.HoldStart).ToList();

            foreach (var holdStart in holdStarts)
            {
                if (holdStart.HoldPartner == null) continue;

                var holdEnd = holdStart.HoldPartner;
                var startDelta = holdStart.TimeSeconds - _engine.CurrentTimeSeconds;
                var endDelta = holdEnd.TimeSeconds - _engine.CurrentTimeSeconds;

                bool isActiveHold = holdStart.Consumed && holdStart.IsHoldActive && !holdEnd.Consumed;
                bool isUpcomingHold = !holdStart.Consumed;

                if (!isActiveHold && !isUpcomingHold) continue;
                if (endDelta < -PhoenixScoring.GetBadWindow(JudgmentDifficulty.Standard)) continue;

                var startNormalized = (float)(startDelta / scrollWindowSeconds);
                var endNormalized = (float)(endDelta / scrollWindowSeconds);

                float visibleStartY, visibleEndY;

                if (isActiveHold)
                {
                    visibleStartY = receptorY;
                    var endY = receptorY + endNormalized * travelHeight;
                    visibleEndY = Math.Min(endY, fieldBottom - 18f);
                }
                else
                {
                    var startY = receptorY + startNormalized * travelHeight;
                    var endY = receptorY + endNormalized * travelHeight;
                    visibleStartY = Math.Max(startY, receptorY - 16f);
                    visibleEndY = Math.Min(endY, fieldBottom - 18f);
                }

                if (visibleStartY >= visibleEndY) continue;

                var holdWidth = IsLandscapeMode ? width * 0.60f : width * 0.65f;
                var isHoldCurrentlyActive = _engine.IsLaneHoldActive(lane);

                Color bodyColor;
                if (isActiveHold && isHoldCurrentlyActive)
                {
                    var pulseIntensity = 0.8f + 0.2f * MathF.Sin((float)_engine.CurrentTimeSeconds * 8f);
                    bodyColor = LaneColors[laneColorIndex].WithAlpha(pulseIntensity);
                }
                else if (isActiveHold)
                {
                    bodyColor = LaneColors[laneColorIndex].WithAlpha(0.4f);
                }
                else
                {
                    bodyColor = LaneColors[laneColorIndex].WithAlpha(0.6f);
                }

                canvas.SaveState();
                canvas.FillColor = bodyColor;
                canvas.FillRoundedRectangle(
                    centerX - holdWidth / 2f,
                    visibleStartY,
                    holdWidth,
                    visibleEndY - visibleStartY,
                    4f);

                var edgeAlpha = isActiveHold && isHoldCurrentlyActive ? 1.0f : 0.8f;
                canvas.StrokeColor = LaneColors[laneColorIndex].WithAlpha(edgeAlpha);
                canvas.StrokeSize = isActiveHold && isHoldCurrentlyActive ? 3f : 2f;
                canvas.DrawRoundedRectangle(
                    centerX - holdWidth / 2f,
                    visibleStartY,
                    holdWidth,
                    visibleEndY - visibleStartY,
                    4f);

                canvas.RestoreState();
            }

            x += width + laneGap;
        }
    }

    private void DrawNotes(ICanvas canvas, float[] actualWidths, float laneGap, float receptorY, float fieldBottom, int laneCount, double scrollWindowSeconds)
    {
        float x = laneGap;
        var travelHeight = fieldBottom - receptorY - 18f;

        for (var lane = 0; lane < laneCount; lane++)
        {
            float width = actualWidths[lane];
            var centerX = x + width / 2f;
            var laneShapeIndex = lane % 5;

            foreach (var note in _engine.Notes.Where(n => n.Lane == lane && !n.Consumed))
            {
                var deltaSeconds = note.TimeSeconds - _engine.CurrentTimeSeconds;

                if (deltaSeconds < -PhoenixScoring.GetBadWindow(_engine.JudgmentDifficulty) || deltaSeconds > scrollWindowSeconds)
                    continue;

                var normalized = (float)(deltaSeconds / scrollWindowSeconds);
                var y = receptorY + normalized * travelHeight;

                float size;
                if (IsLandscapeMode)
                    size = MathF.Min(width * 0.75f, 35f);
                else
                    size = MathF.Min(width * 0.80f, 44f);

                canvas.SaveState();
                canvas.Translate(centerX, y);

                switch (note.Type)
                {
                    case NoteType.Tap:
                        DrawNoteShape(canvas, laneShapeIndex, size, LaneColors[laneShapeIndex]);
                        break;

                    case NoteType.HoldStart:
                        DrawNoteShape(canvas, laneShapeIndex, size * 1.1f, LaneColors[laneShapeIndex]);
                        if (ShowNoteBorders)
                        {
                            canvas.StrokeColor = Colors.White;
                            canvas.StrokeSize = 3f;
                            if (laneShapeIndex == 2)
                                canvas.DrawRectangle(-size * 0.6f, -size * 0.6f, size * 1.2f, size * 1.2f);
                            else
                                DrawDiagonalArrow(canvas, laneShapeIndex, size * 1.2f, strokeOnly: true);
                        }
                        break;

                    case NoteType.HoldEnd:
                        DrawNoteShape(canvas, laneShapeIndex, size, LaneColors[laneShapeIndex].WithAlpha(0.8f));
                        break;

                    case NoteType.HoldBody:
                        canvas.FillColor = LaneColors[laneShapeIndex].WithAlpha(0.6f);
                        canvas.FillEllipse(-size * 0.2f, -size * 0.2f, size * 0.4f, size * 0.4f);
                        break;
                }

                canvas.RestoreState();
            }
            x += width + laneGap;
        }
    }

    private void DrawReceptors(ICanvas canvas, float[] actualWidths, float laneGap, float receptorY, int laneCount)
    {
        float x = laneGap;
        for (var lane = 0; lane < laneCount; lane++)
        {
            float width = actualWidths[lane];
            var centerX = x + width / 2f;
            var flashAge = _engine.GetLaneFlashAge(lane);
            var isHoldActive = _engine.IsLaneHoldActive(lane);
            var glow = flashAge >= 0d && flashAge <= 0.14d ? (float)(1d - flashAge / 0.14d) : 0f;

            if (isHoldActive)
            {
                var pulseGlow = 0.6f + 0.4f * MathF.Sin((float)_engine.CurrentTimeSeconds * 6f);
                glow = Math.Max(glow, pulseGlow);
            }

            float receptorSize;
            if (IsLandscapeMode)
                receptorSize = MathF.Min(width * 0.75f, 38f);
            else
                receptorSize = MathF.Min(width * 0.80f, 44f);

            // Shape/color index is always within the 5-lane pattern
            var laneShapeIndex = lane % 5;

            canvas.SaveState();
            canvas.Translate(centerX, receptorY);

            if (isHoldActive)
            {
                var glowSize = receptorSize * 1.3f;
                canvas.FillColor = Colors.White.WithAlpha(0.06f);
                if (laneShapeIndex == 2)
                    canvas.FillRoundedRectangle(-glowSize / 2f, -glowSize / 2f, glowSize, glowSize, 8f);
                else
                    canvas.FillEllipse(-glowSize / 2f, -glowSize / 2f, glowSize, glowSize);
            }

            var lastJudgment = _engine.GetLaneLastJudgment(lane);
            var isStarworthy = (lastJudgment == HitJudgment.Perfect || lastJudgment == HitJudgment.Great)
                               && flashAge >= 0d && flashAge <= 0.30d;

            if (isStarworthy)
                DrawStarBurst(canvas, laneShapeIndex, receptorSize, flashAge, lastJudgment);

            DrawReceptorShape(canvas, laneShapeIndex, receptorSize, glow, isHoldActive);

            if (glow > 0f)
            {
                var pulse = 0.9f + 0.05f * MathF.Sin((float)_engine.CurrentTimeSeconds * 18f);
                var yellowAlpha = Math.Clamp(glow * 0.5f * pulse, 0f, 1f);

                canvas.SaveState();
                canvas.FillColor = Color.FromArgb("#FFE45E").WithAlpha(yellowAlpha);

                if (laneShapeIndex == 2)
                {
                    var haloSize = receptorSize * 1.2f;
                    canvas.FillRoundedRectangle(-haloSize / 2f, -haloSize / 2f, haloSize, haloSize, 8f);
                }
                else
                {
                    var haloSize = receptorSize * 1.3f;
                    canvas.FillEllipse(-haloSize / 2f, -haloSize / 2f, haloSize, haloSize);
                }

                canvas.RestoreState();
            }

            canvas.RestoreState();
            x += width + laneGap;
        }
    }

    private void DrawReceptorShape(ICanvas canvas, int lane, float size, float glow, bool isHoldActive)
    {
        // Receptors should use grayscale assets per user request:
        // lane 0 (leftmost)  -> gray_prime rotated -90
        // lane 1            -> gray_prime rotated 0
        // lane 2 (center)   -> grayc_prime (or skin variant)
        // lane 3            -> gray_prime rotated 90
        // lane 4 (rightmost)-> gray_prime rotated 180

        if (lane == 2)
        {
            // center receptor uses grayc_* asset if available
            var grayCenter = GetGrayCenterImage();
            if (grayCenter != null)
            {
                canvas.Alpha = 0.90f + glow * 0.10f;
                canvas.DrawImage(grayCenter, -size / 2f, -size / 2f, size, size);
                canvas.Alpha = 1f;

                if (ShowNoteBorders)
                {
                    canvas.StrokeColor = Colors.White.WithAlpha(0.55f + glow * 0.25f);
                    canvas.StrokeSize = isHoldActive ? 4f : 3f;
                    canvas.DrawRectangle(-size / 2f, -size / 2f, size, size);
                }

                return;
            }

            // Fallback to previous color center image if gray center not available
            var centerImage = GetCenterImage();
            if (centerImage != null)
            {
                canvas.Alpha = 0.20f + glow * 0.40f;
                canvas.DrawImage(centerImage, -size / 2f, -size / 2f, size, size);
                canvas.Alpha = 1f;

                if (ShowNoteBorders)
                {
                    canvas.StrokeColor = LaneColors[lane].WithAlpha(0.70f + glow * 0.25f);
                    canvas.StrokeSize = isHoldActive ? 4f : 3f;
                    canvas.DrawRectangle(-size / 2f, -size / 2f, size, size);
                }
            }
            else
            {
                // Fallback to drawn square
                canvas.FillColor = Colors.White.WithAlpha(0.18f + glow * 0.4f);
                canvas.FillRectangle(-size / 2f, -size / 2f, size, size);
                if (ShowNoteBorders)
                {
                    canvas.StrokeColor = Colors.White.WithAlpha(0.7f);
                    canvas.StrokeSize = 2f;
                    canvas.DrawRectangle(-size / 2f, -size / 2f, size, size);
                }
            }
        }
        else
        {
            // Non-center receptors: use gray image rotated per lane mapping
            DrawGrayReceptor(canvas, lane, size, glow, isHoldActive);
        }
    }

    private void DrawGrayReceptor(ICanvas canvas, int lane, float size, float glow, bool isHoldActive)
    {
        var gray = GetGrayImage();

        // rotation mapping:
        // lane 0 -> -90
        // lane 1 -> 0
        // lane 3 -> 90
        // lane 4 -> 180
        float rotation = lane switch
        {
            0 => -90f,
            1 => 0f,
            3 => 90f,
            4 => 180f,
            _ => 0f
        };

        if (gray != null)
        {
            canvas.SaveState();

            if (rotation != 0f)
            {
                canvas.Rotate(rotation);
            }

            // Use normal (full) alpha for non-center grayscale receptors per request
            canvas.Alpha = 1f;
            canvas.DrawImage(gray, -size / 2f, -size / 2f, size, size);
            canvas.Alpha = 1f;
            canvas.RestoreState();

            if (ShowNoteBorders)
            {
                canvas.StrokeColor = Colors.White.WithAlpha(0.70f + glow * 0.25f);
                canvas.StrokeSize = isHoldActive ? 4f : 3f;
                canvas.DrawEllipse(-size / 2f, -size / 2f, size, size);
            }

            return;
        }

        // If no gray image found, fallback to colored receptor rendering
        IImage? image = null;
        float fallbackRotation = 0f;

        switch (lane)
        {
            case 0:
                image = GetBlueArrowImage();
                fallbackRotation = -90f;
                break;
            case 1:
                image = GetRedArrowImage();
                fallbackRotation = 0f;
                break;
            case 3:
                image = GetRedArrowImage();
                fallbackRotation = 90f;
                break;
            case 4:
                image = GetBlueArrowImage();
                fallbackRotation = 180f;
                break;
        }

        if (image != null)
        {
            canvas.SaveState();
            if (fallbackRotation != 0f) canvas.Rotate(fallbackRotation);
            canvas.Alpha = 0.20f + glow * 0.40f;
            canvas.DrawImage(image, -size / 2f, -size / 2f, size, size);
            canvas.Alpha = 1f;
            canvas.RestoreState();

            if (ShowNoteBorders)
            {
                canvas.StrokeColor = Colors.White.WithAlpha(0.70f + glow * 0.25f);
                canvas.StrokeSize = isHoldActive ? 4f : 3f;
                canvas.DrawEllipse(-size / 2f, -size / 2f, size, size);
            }
        }
        else
        {
            // final fallback: drawn diagonal arrow (colored)
            canvas.FillColor = LaneColors[lane].WithAlpha(0.20f + glow * 0.40f);

            if (ShowNoteBorders)
            {
                canvas.StrokeColor = Colors.White.WithAlpha(0.70f + glow * 0.25f);
                canvas.StrokeSize = isHoldActive ? 4f : 3f;
            }

            DrawDiagonalArrow(canvas, lane, size);
        }
    }

    private void DrawNoteShape(ICanvas canvas, int lane, float size, Color color)
    {
        if (lane == 2)
        {
            var centerImage = GetCenterImage();
            if (centerImage != null)
            {
                canvas.DrawImage(centerImage, -size / 2f, -size / 2f, size, size);
                if (ShowNoteBorders)
                {
                    canvas.StrokeColor = Colors.White.WithAlpha(0.80f);
                    canvas.StrokeSize = 2f;
                    canvas.DrawRectangle(-size / 2f, -size / 2f, size, size);
                }
            }
            else
            {
                canvas.FillColor = color;
                canvas.FillRectangle(-size / 2f, -size / 2f, size, size);
                if (ShowNoteBorders)
                {
                    canvas.StrokeColor = Colors.White.WithAlpha(0.80f);
                    canvas.StrokeSize = 2f;
                    canvas.DrawRectangle(-size / 2f, -size / 2f, size, size);
                }
            }
        }
        else
        {
            DrawArrowNote(canvas, lane, size, color);
        }
    }

    private void DrawArrowNote(ICanvas canvas, int lane, float size, Color color)
    {
        IImage? image = null;
        float rotation = 0f;

        switch (lane)
        {
            case 0: image = GetBlueArrowImage(); break;
            case 1: image = GetRedArrowImage(); break;
            case 3: image = GetRedArrowImage(); rotation = 90f; break;
            case 4: image = GetBlueArrowImage(); rotation = -90f; break;
        }

        if (image != null)
        {
            canvas.SaveState();
            if (rotation != 0f)
                canvas.Rotate(rotation);
            canvas.DrawImage(image, -size / 2f, -size / 2f, size, size);
            canvas.RestoreState();

            if (ShowNoteBorders)
            {
                canvas.StrokeColor = Colors.White.WithAlpha(0.80f);
                canvas.StrokeSize = 2f;
                canvas.DrawEllipse(-size / 2f, -size / 2f, size, size);
            }
        }
        else
        {
            canvas.FillColor = color;
            if (ShowNoteBorders)
            {
                canvas.StrokeColor = Colors.White.WithAlpha(0.80f);
                canvas.StrokeSize = 2f;
            }
            DrawDiagonalArrow(canvas, lane, size);
        }
    }

    private static void DrawDiagonalArrow(ICanvas canvas, int lane, float size, bool strokeOnly = false)
    {
        var path = new PathF();
        var halfSize = size / 2f;

        switch (lane)
        {
            case 0: // ↙ Bottom-left
                path.MoveTo(halfSize * 0.3f, -halfSize * 0.8f);
                path.LineTo(halfSize * 0.8f, -halfSize * 0.3f);
                path.LineTo(halfSize * 0.5f, -halfSize * 0.1f);
                path.LineTo(-halfSize * 0.1f, halfSize * 0.5f);
                path.LineTo(-halfSize * 0.3f, halfSize * 0.8f);
                path.LineTo(-halfSize, halfSize * 0.5f);
                path.LineTo(-halfSize * 0.5f, halfSize);
                path.LineTo(-halfSize * 0.8f, halfSize * 0.3f);
                path.LineTo(-halfSize * 0.6f, halfSize * 0.1f);
                path.LineTo(halfSize * 0.1f, -halfSize * 0.6f);
                path.Close();
                break;

            case 1: // ↖ Top-left
                path.MoveTo(halfSize * 0.3f, halfSize * 0.8f);
                path.LineTo(halfSize * 0.8f, halfSize * 0.3f);
                path.LineTo(halfSize * 0.5f, halfSize * 0.1f);
                path.LineTo(-halfSize * 0.1f, -halfSize * 0.5f);
                path.LineTo(-halfSize * 0.3f, -halfSize * 0.8f);
                path.LineTo(-halfSize, -halfSize * 0.5f);
                path.LineTo(-halfSize * 0.5f, -halfSize);
                path.LineTo(-halfSize * 0.8f, -halfSize * 0.3f);
                path.LineTo(-halfSize * 0.6f, -halfSize * 0.1f);
                path.LineTo(halfSize * 0.1f, halfSize * 0.6f);
                path.Close();
                break;

            case 3: // ↗ Top-right
                path.MoveTo(-halfSize * 0.3f, halfSize * 0.8f);
                path.LineTo(-halfSize * 0.8f, halfSize * 0.3f);
                path.LineTo(-halfSize * 0.5f, halfSize * 0.1f);
                path.LineTo(halfSize * 0.1f, -halfSize * 0.5f);
                path.LineTo(halfSize * 0.3f, -halfSize * 0.8f);
                path.LineTo(halfSize, -halfSize * 0.5f);
                path.LineTo(halfSize * 0.5f, -halfSize);
                path.LineTo(halfSize * 0.8f, -halfSize * 0.3f);
                path.LineTo(halfSize * 0.6f, -halfSize * 0.1f);
                path.LineTo(-halfSize * 0.1f, halfSize * 0.6f);
                path.Close();
                break;

            case 4: // ↘ Bottom-right
                path.MoveTo(-halfSize * 0.3f, -halfSize * 0.8f);
                path.LineTo(-halfSize * 0.8f, -halfSize * 0.3f);
                path.LineTo(-halfSize * 0.5f, -halfSize * 0.1f);
                path.LineTo(halfSize * 0.1f, halfSize * 0.5f);
                path.LineTo(halfSize * 0.3f, halfSize * 0.8f);
                path.LineTo(halfSize, halfSize * 0.5f);
                path.LineTo(halfSize * 0.5f, halfSize);
                path.LineTo(halfSize * 0.8f, halfSize * 0.3f);
                path.LineTo(halfSize * 0.6f, halfSize * 0.1f);
                path.LineTo(-halfSize * 0.1f, -halfSize * 0.6f);
                path.Close();
                break;
        }

        if (strokeOnly)
        {
            canvas.DrawPath(path);
        }
        else
        {
            canvas.FillPath(path);
            if (!strokeOnly)
            {
                canvas.DrawPath(path);
            }
        }
    }

    private static void DrawFrame(ICanvas canvas, RectF dirtyRect, float receptorY, float topMargin)
    {
        canvas.StrokeColor = Colors.White.WithAlpha(0.18f);
        canvas.StrokeSize = 3f;
        canvas.DrawRoundedRectangle(4f, topMargin - 8f, dirtyRect.Width - 8f, dirtyRect.Height - topMargin - 8f, 18f);

        canvas.FontColor = Colors.White.WithAlpha(0.90f);
        canvas.FontSize = 14f;
    }

    /// <summary>
    /// Draws a bright lens-flare style cross-light effect centred at the current canvas origin.
    /// </summary>
    private static void DrawStarBurst(ICanvas canvas, int lane, float receptorSize, double flashAge, HitJudgment judgment)
    {
        var t = (float)(flashAge / 0.25d);

        var alpha = Math.Clamp(1.0f - t, 0f, 1f);
        // Arm length: starts at 0.65× receptor size, expands to 1.5× (was 1.0× → 2.8×)
        var armLength = receptorSize * (0.65f + 0.85f * t);
        // Arm width: starts slightly thinner, tapers the same way
        var armWidth = receptorSize * Math.Clamp(0.22f - 0.18f * t, 0.03f, 0.22f);

        canvas.SaveState();

        // Soft outer glow halo — pure white, very transparent
        var glowRadius = armLength * 0.55f;
        canvas.FillColor = Colors.White.WithAlpha(alpha * 0.18f);
        canvas.FillEllipse(-glowRadius, -glowRadius, glowRadius * 2f, glowRadius * 2f);

        // Draw two crossing lines at 0° and 90°, rotated 45° for the × look
        canvas.Rotate(45f);

        canvas.StrokeColor = Colors.White.WithAlpha(alpha);
        canvas.StrokeSize = armWidth;
        canvas.StrokeLineCap = LineCap.Round;

        // Horizontal arm
        canvas.DrawLine(-armLength, 0f, armLength, 0f);
        // Vertical arm
        canvas.DrawLine(0f, -armLength, 0f, armLength);

        // Second, slightly narrower pass with higher alpha for the bright core streak
        var coreAlpha = Math.Clamp((1.0f - t) * 1.6f, 0f, 1f);
        var coreLength = armLength * 0.55f;
        canvas.StrokeColor = Colors.White.WithAlpha(coreAlpha);
        canvas.StrokeSize = armWidth * 0.45f;
        canvas.DrawLine(-coreLength, 0f, coreLength, 0f);
        canvas.DrawLine(0f, -coreLength, 0f, coreLength);

        canvas.RestoreState();

        // Bright white core dot that flashes at the centre and shrinks fast
        var dotAlpha = Math.Clamp((1.0f - t) * 2.0f, 0f, 1f);
        var dotRadius = receptorSize * 0.18f * (1.0f - t * 0.8f);
        canvas.FillColor = Colors.White.WithAlpha(dotAlpha);
        canvas.FillEllipse(-dotRadius, -dotRadius, dotRadius * 2f, dotRadius * 2f);
    }
}
