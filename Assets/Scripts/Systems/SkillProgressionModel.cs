using UnityEngine;

public static class SkillProgressionModel
{
    public const int MinLevel = 0;
    public const int MaxLevel = 10;

    private static readonly int[] LevelTransitionCosts =
    {
        100, 160, 256, 410, 656, 1050, 1680, 2688, 4300, 6880
    };

    public static int GetTransitionCount()
    {
        return LevelTransitionCosts.Length;
    }

    public static int GetRequiredSPForNextLevel(int level)
    {
        int normalizedLevel = Mathf.Clamp(level, MinLevel, MaxLevel);
        if (normalizedLevel >= MaxLevel)
        {
            return 0;
        }

        return LevelTransitionCosts[normalizedLevel - MinLevel];
    }

    public static int GetTotalSPForLevelStart(int level)
    {
        int normalizedLevel = Mathf.Clamp(level, MinLevel, MaxLevel);
        int total = 0;
        for (int i = 0; i < normalizedLevel - MinLevel; i++)
        {
            total += LevelTransitionCosts[i];
        }

        return total;
    }

    public static int GetTotalSPForMaxLevel()
    {
        return GetTotalSPForLevelStart(MaxLevel);
    }

    public static int ClampTotalSP(int totalSP)
    {
        return Mathf.Clamp(totalSP, 0, GetTotalSPForMaxLevel());
    }

    public static int GetLevel(int totalSP)
    {
        int clampedTotalSP = ClampTotalSP(totalSP);
        int currentLevel = MinLevel;
        int remainingSP = clampedTotalSP;

        for (int i = 0; i < LevelTransitionCosts.Length; i++)
        {
            if (remainingSP < LevelTransitionCosts[i])
            {
                break;
            }

            remainingSP -= LevelTransitionCosts[i];
            currentLevel++;
        }

        return Mathf.Clamp(currentLevel, MinLevel, MaxLevel);
    }

    public static int GetProgressInLevel(int totalSP)
    {
        int clampedTotalSP = ClampTotalSP(totalSP);
        if (IsMaxed(clampedTotalSP))
        {
            return 0;
        }

        int level = GetLevel(clampedTotalSP);
        return clampedTotalSP - GetTotalSPForLevelStart(level);
    }

    public static float GetProgressInLevel01(int totalSP)
    {
        int clampedTotalSP = ClampTotalSP(totalSP);
        if (IsMaxed(clampedTotalSP))
        {
            return 1f;
        }

        int level = GetLevel(clampedTotalSP);
        int required = GetRequiredSPForNextLevel(level);
        if (required <= 0)
        {
            return 1f;
        }

        return Mathf.Clamp01(GetProgressInLevel(clampedTotalSP) / (float)required);
    }

    public static float GetAxisPercent(int totalSP)
    {
        int clampedTotalSP = ClampTotalSP(totalSP);
        if (IsMaxed(clampedTotalSP))
        {
            return 100f;
        }

        int level = GetLevel(clampedTotalSP);
        int completedLevels = Mathf.Max(0, level - MinLevel);
        float progressInLevel01 = GetProgressInLevel01(clampedTotalSP);
        return Mathf.Clamp(completedLevels * 10f + progressInLevel01 * 10f, 0f, 100f);
    }

    public static bool IsMaxed(int totalSP)
    {
        return ClampTotalSP(totalSP) >= GetTotalSPForMaxLevel();
    }

    public static int GetTotalSPForAxisPercent(float axisPercent)
    {
        float clampedAxisPercent = Mathf.Clamp(axisPercent, 0f, 100f);
        if (clampedAxisPercent >= 100f)
        {
            return GetTotalSPForMaxLevel();
        }

        int completedBands = Mathf.FloorToInt(clampedAxisPercent / 10f);
        float bandProgress01 = (clampedAxisPercent - completedBands * 10f) / 10f;
        int level = Mathf.Clamp(completedBands + MinLevel, MinLevel, MaxLevel - 1);
        int totalForCompletedBands = GetTotalSPForLevelStart(level);
        int nextLevelCost = GetRequiredSPForNextLevel(level);
        return ClampTotalSP(totalForCompletedBands + Mathf.RoundToInt(nextLevelCost * bandProgress01));
    }

    public static int GetDeltaSPForAxisGain(int currentTotalSP, float axisGainPercent)
    {
        int clampedCurrentTotalSP = ClampTotalSP(currentTotalSP);
        if (axisGainPercent <= 0f || IsMaxed(clampedCurrentTotalSP))
        {
            return 0;
        }

        float currentAxisPercent = GetAxisPercent(clampedCurrentTotalSP);
        float targetAxisPercent = Mathf.Clamp(currentAxisPercent + axisGainPercent, 0f, 100f);
        int targetTotalSP = GetTotalSPForAxisPercent(targetAxisPercent);
        return Mathf.Max(0, targetTotalSP - clampedCurrentTotalSP);
    }
}
