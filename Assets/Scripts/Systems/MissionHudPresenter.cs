using System;

public readonly struct MissionHudSlotViewData
{
    public MissionHudSlotViewData(string displayText, bool canClaim)
    {
        DisplayText = displayText;
        CanClaim = canClaim;
    }

    public string DisplayText { get; }
    public bool CanClaim { get; }
}

public static class MissionHudPresenter
{
    public static MissionHudSlotViewData Build(MissionEntryData mission)
    {
        if (mission == null)
        {
            return new MissionHudSlotViewData("No mission", false);
        }

        string status = mission.isClaimed ? " [Claimed]" : (mission.isCompleted ? " [Completed]" : string.Empty);
        string displayText = $"{GetMissionTitleLabel(mission)}: {GetMissionProgressLabel(mission)}{status}";
        bool canClaim = mission.isCompleted && !mission.isClaimed;
        return new MissionHudSlotViewData(displayText, canClaim);
    }

    private static string GetMissionProgressLabel(MissionEntryData mission)
    {
        if (mission == null)
        {
            return "0 / 0";
        }

        if (string.Equals(mission.skillMissionMode, "sessions", StringComparison.Ordinal))
        {
            return $"{mission.currentProgress} / {mission.targetProgress} sessions";
        }

        if (mission.requiredMinutes > 0f)
        {
            return $"{mission.progressMinutes:0.#} / {mission.requiredMinutes:0.#} min";
        }

        return $"{mission.currentProgress} / {mission.targetProgress}";
    }

    private static string GetMissionTitleLabel(MissionEntryData mission)
    {
        if (mission == null)
        {
            return "Mission";
        }

        if (!string.IsNullOrEmpty(mission.title))
        {
            return mission.title;
        }

        string skillName = !string.IsNullOrEmpty(mission.targetSkillName)
            ? mission.targetSkillName
            : "Unknown Skill";

        if (string.Equals(mission.skillMissionMode, "sessions", StringComparison.Ordinal))
        {
            return $"Complete {mission.targetProgress} focus sessions on {skillName}";
        }

        if (!string.IsNullOrEmpty(mission.targetSkillId) || !string.IsNullOrEmpty(mission.skillId))
        {
            float targetMinutes = mission.requiredMinutes > 0f ? mission.requiredMinutes : mission.targetProgress;
            return $"Focus {targetMinutes:0.#} min on {skillName}";
        }

        return "Mission";
    }
}
