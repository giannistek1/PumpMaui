namespace PumpMaui.Game;

public static class PhoenixScoring
{
    public const double PerfectWindowSeconds = 0.045d;
    public const double GreatWindowSeconds = 0.090d;
    public const double GoodWindowSeconds = 0.135d;
    public const double BadWindowSeconds = 0.180d;
    public const int MaxScore = 1_000_000;

    public static HitJudgment GetJudgment(double deltaSeconds)
    {
        var absoluteDelta = Math.Abs(deltaSeconds);
        if (absoluteDelta <= PerfectWindowSeconds)
        {
            return HitJudgment.Perfect;
        }

        if (absoluteDelta <= GreatWindowSeconds)
        {
            return HitJudgment.Great;
        }

        if (absoluteDelta <= GoodWindowSeconds)
        {
            return HitJudgment.Good;
        }

        if (absoluteDelta <= BadWindowSeconds)
        {
            return HitJudgment.Bad;
        }

        return HitJudgment.Miss;
    }

    public static double GetWeight(HitJudgment judgment)
    {
        return judgment switch
        {
            HitJudgment.Perfect => 1.00d,
            HitJudgment.Great => 0.82d,
            HitJudgment.Good => 0.55d,
            HitJudgment.Bad => 0.20d,
            _ => 0d
        };
    }

    public static bool BreaksCombo(HitJudgment judgment)
    {
        return judgment is HitJudgment.Bad or HitJudgment.Miss;
    }

    public static int CalculateScore(IReadOnlyDictionary<HitJudgment, int> counts, int noteCount)
    {
        if (noteCount <= 0)
        {
            return 0;
        }

        var totalWeight = counts.Sum(pair => pair.Value * GetWeight(pair.Key));
        return (int)Math.Round(totalWeight / noteCount * MaxScore, MidpointRounding.AwayFromZero);
    }

    public static string CalculateGrade(int score)
    {
        return score switch
        {
            >= 995000 => "SSS+",
            >= 990000 => "SSS",
            >= 985000 => "SS+",
            >= 980000 => "SS",
            >= 975000 => "S+",
            >= 970000 => "S",
            >= 960000 => "AAA+",
            >= 950000 => "AAA",
            >= 925000 => "AA+",
            >= 900000 => "AA",
            >= 825000 => "A+",
            >= 750000 => "A",
            >= 700000 => "B",
            >= 600000 => "C",
            >= 450000 => "D",
            _ => "F"
        };
    }

    public static string CalculatePlate(IReadOnlyDictionary<HitJudgment, int> counts, int noteCount)
    {
        var missCount = counts[HitJudgment.Miss];
        var badCount = counts[HitJudgment.Bad];
        var goodCount = counts[HitJudgment.Good];
        var greatCount = counts[HitJudgment.Great];
        var perfectCount = counts[HitJudgment.Perfect];

        // Perfect Game: ALL PERFECT
        if (perfectCount == noteCount)
        {
            return "Perfect Game";
        }

        // Ultimate Game: GOOD+BAD+MISS=0
        if (goodCount == 0 && badCount == 0 && missCount == 0)
        {
            return "Ultimate Game";
        }

        // Extreme Game: BAD+MISS=0
        if (badCount == 0 && missCount == 0)
        {
            return "Extreme Game";
        }

        // SuperB Game: 0 misses
        if (missCount == 0)
        {
            return "SuperB Game";
        }

        // Marvelous Game: 1 to 5 misses
        if (missCount >= 1 && missCount <= 5)
        {
            return "Marvelous Game";
        }

        // Talented Game: 6 to 10 misses
        if (missCount >= 6 && missCount <= 10)
        {
            return "Talented Game";
        }

        // Fair Game: 11 to 20 misses
        if (missCount >= 11 && missCount <= 20)
        {
            return "Fair Game";
        }

        // Rough Game: 21 or more misses
        return "Rough Game";
    }

    public static Color GetGradeColor(string grade)
    {
        return grade switch
        {
            "SSS+" or "SSS" => Color.FromArgb("#87CEEB"), // Diamond (Sky Blue)
            "SS+" or "SS" or "S+" or "S" => Color.FromArgb("#FFD700"), // Gold
            "AAA+" or "AAA" => Color.FromArgb("#C0C0C0"), // Silver
            "AA+" or "AA" or "A+" or "A" => Color.FromArgb("#CD7F32"), // Bronze
            "B" => Color.FromArgb("#4169E1"), // Royal Blue
            "C" => Color.FromArgb("#32CD32"), // Lime Green
            "D" => Color.FromArgb("#9ACD32"), // Yellow Green
            "F" => Color.FromArgb("#228B22"), // Forest Green
            _ => Color.FromArgb("#FFFFFF") // White fallback
        };
    }

    public static Color GetPlateColor(string plate)
    {
        return plate switch
        {
            "Perfect Game" or "Ultimate Game" => Color.FromArgb("#87CEEB"), // Diamond (Sky Blue)
            "Extreme Game" or "SuperB Game" => Color.FromArgb("#FFD700"), // Gold
            "Marvelous Game" or "Talented Game" => Color.FromArgb("#C0C0C0"), // Silver
            "Fair Game" or "Rough Game" => Color.FromArgb("#CD7F32"), // Bronze
            _ => Color.FromArgb("#FFFFFF") // White fallback
        };
    }
}
