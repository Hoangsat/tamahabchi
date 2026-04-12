using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public readonly struct BattlePlayerPreviewViewData
{
    public BattlePlayerPreviewViewData(string powerText, string skillsText)
    {
        PowerText = powerText;
        SkillsText = skillsText;
    }

    public string PowerText { get; }
    public string SkillsText { get; }
}

public readonly struct BattleBossDetailViewData
{
    public BattleBossDetailViewData(string nameText, string metaText, string powerText, bool canFight, string fightButtonText, List<float> radarValues, List<Color> radarColors)
    {
        NameText = nameText;
        MetaText = metaText;
        PowerText = powerText;
        CanFight = canFight;
        FightButtonText = fightButtonText;
        RadarValues = radarValues;
        RadarColors = radarColors;
    }

    public string NameText { get; }
    public string MetaText { get; }
    public string PowerText { get; }
    public bool CanFight { get; }
    public string FightButtonText { get; }
    public List<float> RadarValues { get; }
    public List<Color> RadarColors { get; }
}

public readonly struct BattleResultViewData
{
    public BattleResultViewData(string text, Color color)
    {
        Text = text;
        Color = color;
    }

    public string Text { get; }
    public Color Color { get; }
}

public readonly struct BattleBossListEntryViewData
{
    public BattleBossListEntryViewData(string labelText, Color backgroundColor, Color labelColor)
    {
        LabelText = labelText;
        BackgroundColor = backgroundColor;
        LabelColor = labelColor;
    }

    public string LabelText { get; }
    public Color BackgroundColor { get; }
    public Color LabelColor { get; }
}

public static class BattlePanelPresenter
{
    private static readonly Color ResultIdleColor = new Color(0.83f, 0.89f, 0.96f, 0.92f);
    private static readonly Color ResultBlockedColor = new Color(1f, 0.84f, 0.45f, 1f);
    private static readonly Color ResultWinColor = new Color(0.48f, 0.92f, 0.62f, 1f);
    private static readonly Color ResultLossColor = new Color(1f, 0.58f, 0.48f, 1f);
    private static readonly Color SelectedBossBackgroundColor = new Color(0.38f, 0.28f, 0.2f, 0.98f);
    private static readonly Color UnselectedBossBackgroundColor = new Color(0.16f, 0.2f, 0.28f, 0.96f);
    private static readonly Color SelectedBossLabelColor = new Color(1f, 0.91f, 0.74f, 1f);
    private static readonly Color UnselectedBossLabelColor = new Color(0.9f, 0.94f, 1f, 0.96f);
    private static readonly Color[] BossPalette =
    {
        new Color(0.98f, 0.64f, 0.31f, 1f),
        new Color(0.93f, 0.34f, 0.29f, 1f),
        new Color(0.92f, 0.83f, 0.31f, 1f),
        new Color(0.65f, 0.78f, 0.98f, 1f),
        new Color(0.68f, 0.93f, 0.85f, 1f)
    };

    public static BattlePlayerPreviewViewData BuildPlayerPreview(BattlePlayerPreviewData preview, BattleAvailabilityData availability)
    {
        if (preview == null)
        {
            return new BattlePlayerPreviewViewData("Player Battle Power: --", "No combat skills yet.\nAdd skills and grow them through focus to raise your power.");
        }

        string powerText = $"Player Battle Power: {FormatNumber(preview.playerBattlePower)}";
        if (preview.combatSkills == null || preview.combatSkills.Count == 0)
        {
            return new BattlePlayerPreviewViewData(powerText, "No combat skills yet.\nAdd skills and grow them through focus to raise your power.");
        }

        List<string> lines = new List<string>(preview.combatSkills.Count + 2);
        for (int i = 0; i < preview.combatSkills.Count; i++)
        {
            BattleCombatSkillSnapshotData skill = preview.combatSkills[i];
            if (skill == null)
            {
                continue;
            }

            string icon = string.IsNullOrEmpty(skill.icon) ? "*" : skill.icon;
            lines.Add($"{icon} {skill.name}  |  Lv.{skill.level}  |  {skill.effectiveSP} Power  |  Axis {FormatNumber(skill.axisPercent)}%");
        }

        if (availability != null && !availability.HasEnoughSkills)
        {
            lines.Add(string.Empty);
            lines.Add($"Need {availability.requiredSkillCount} tracked skills to unlock battle ({availability.trackedSkillCount}/{availability.requiredSkillCount}).");
        }

        return new BattlePlayerPreviewViewData(powerText, lines.Count > 0 ? string.Join("\n", lines) : "No combat skills yet.\nAdd skills and grow them through focus to raise your power.");
    }

    public static BattlePlayerPreviewViewData BuildPlayerPreview(BattlePlayerPreviewData preview)
    {
        return BuildPlayerPreview(preview, null);
    }

    public static BattleBossDetailViewData BuildBossDetail(BossDefinitionData boss, BattleAvailabilityData availability)
    {
        if (boss == null)
        {
            return new BattleBossDetailViewData(
                "Select a boss",
                "Choose a boss from the list.",
                "Boss Power: --",
                false,
                "No Boss",
                null,
                null);
        }

        bool hasRadarValues = boss.visualWebValues01 != null && boss.visualWebValues01.Count >= 3;
        return new BattleBossDetailViewData(
            boss.name,
            $"Target Lv.{boss.targetLevel}  |  {boss.difficultyTier}  |  Win +{boss.rewardCoins} Coins",
            $"Boss Power: {boss.bossPower}",
            availability == null || availability.CanFight,
            GetFightButtonText(availability),
            hasRadarValues ? boss.visualWebValues01 : null,
            hasRadarValues ? BuildRadarColors(boss.visualWebValues01.Count) : null);
    }

    public static BattleBossDetailViewData BuildBossDetail(BossDefinitionData boss)
    {
        return BuildBossDetail(boss, null);
    }

    public static BattleResultViewData BuildResult(BattleResultData result, BossDefinitionData selectedBoss, BattleAvailabilityData availability)
    {
        if (result != null && result.wasBlocked)
        {
            return new BattleResultViewData(
                BuildBlockedCopy(result.statusMessage, result.adviceMessage, result.bossName),
                ResultBlockedColor);
        }

        if (result == null || string.IsNullOrEmpty(result.bossId))
        {
            if (selectedBoss != null && availability != null && !availability.CanFight)
            {
                return new BattleResultViewData(
                    BuildBlockedCopy(availability.blockedReason, availability.guidance, selectedBoss.name),
                    ResultBlockedColor);
            }

            return new BattleResultViewData(
                selectedBoss != null
                    ? $"Ready to challenge {selectedBoss.name}.\nResult is deterministic in v1: your top 3 combat skills vs boss power."
                    : "Select a boss and fight.\nResult is deterministic in v1: your top 3 combat skills vs boss power.",
                ResultIdleColor);
        }

        bool won = result.result == BattleOutcome.Win;
        string bossLabel = string.IsNullOrWhiteSpace(result.bossName)
            ? string.IsNullOrWhiteSpace(selectedBoss != null ? selectedBoss.name : string.Empty) ? "Boss" : selectedBoss.name
            : result.bossName;
        List<string> lines = new List<string>
        {
            won ? $"VICTORY vs {bossLabel}" : $"LOSS vs {bossLabel}",
            $"Player {FormatNumber(result.playerBattlePower)} vs Boss {result.bossPower}",
            $"Energy -{FormatNumber(result.energyCost)}"
        };

        if (won && result.rewardCoins > 0)
        {
            lines.Add($"Rewards +{result.rewardCoins} Coins");
        }
        else if (!won)
        {
            lines.Add("No reward this round.");
        }

        if (!string.IsNullOrWhiteSpace(result.adviceMessage))
        {
            lines.Add(result.adviceMessage);
        }

        return new BattleResultViewData(
            string.Join("\n", lines),
            won ? ResultWinColor : ResultLossColor);
    }

    public static BattleResultViewData BuildResult(BattleResultData result)
    {
        return BuildResult(result, null, null);
    }

    public static BattleBossListEntryViewData BuildBossListEntry(BossDefinitionData boss, bool isSelected)
    {
        if (boss == null)
        {
            return new BattleBossListEntryViewData(string.Empty, UnselectedBossBackgroundColor, UnselectedBossLabelColor);
        }

        return new BattleBossListEntryViewData(
            $"{boss.name}  |  {boss.bossPower}",
            isSelected ? SelectedBossBackgroundColor : UnselectedBossBackgroundColor,
            isSelected ? SelectedBossLabelColor : UnselectedBossLabelColor);
    }

    private static List<Color> BuildRadarColors(int count)
    {
        List<Color> colors = new List<Color>(count);
        for (int i = 0; i < count; i++)
        {
            colors.Add(BossPalette[i % BossPalette.Length]);
        }

        return colors;
    }

    private static string GetFightButtonText(BattleAvailabilityData availability)
    {
        if (availability == null || availability.CanFight)
        {
            return $"Fight (-{FormatNumber(BattleSystem.DefaultBattleEnergyCost)} EN)";
        }

        if (!availability.HasEnoughSkills)
        {
            return $"Need {availability.requiredSkillCount} Skills";
        }

        if (!availability.HasEnoughEnergy)
        {
            return $"Need {FormatNumber(availability.requiredEnergy)} Energy";
        }

        return "Fight";
    }

    private static string BuildBlockedCopy(string blockedReason, string guidance, string bossName)
    {
        List<string> lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(bossName))
        {
            lines.Add($"Cannot challenge {bossName} yet.");
        }

        if (!string.IsNullOrWhiteSpace(blockedReason))
        {
            lines.Add(blockedReason);
        }

        if (!string.IsNullOrWhiteSpace(guidance))
        {
            lines.Add(guidance);
        }

        return lines.Count > 0 ? string.Join("\n", lines) : "Battle is currently unavailable.";
    }

    private static string FormatNumber(float value)
    {
        return value.ToString("0.#", CultureInfo.InvariantCulture);
    }
}
