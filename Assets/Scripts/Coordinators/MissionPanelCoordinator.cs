using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct MissionPanelViewState
{
    public MissionPanelViewState(
        string title,
        string resetInfo,
        string headerStats,
        List<MissionEntryData> skillMissions,
        List<MissionEntryData> routines,
        MissionBonusStatus bonus)
    {
        Title = title ?? string.Empty;
        ResetInfo = resetInfo ?? string.Empty;
        HeaderStats = headerStats ?? string.Empty;
        SkillMissions = skillMissions ?? new List<MissionEntryData>();
        Routines = routines ?? new List<MissionEntryData>();
        Bonus = bonus ?? new MissionBonusStatus();
    }

    public string Title { get; }
    public string ResetInfo { get; }
    public string HeaderStats { get; }
    public List<MissionEntryData> SkillMissions { get; }
    public List<MissionEntryData> Routines { get; }
    public MissionBonusStatus Bonus { get; }
}

public readonly struct MissionPanelActionResult
{
    public MissionPanelActionResult(bool success, string message)
    {
        Success = success;
        Message = message ?? string.Empty;
    }

    public bool Success { get; }
    public string Message { get; }
}

public sealed class MissionPanelCoordinator
{
    private readonly GameManager gameManager;

    public MissionPanelCoordinator(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public MissionPanelViewState GetViewState()
    {
        if (gameManager == null)
        {
            return new MissionPanelViewState(
                "Missions",
                string.Empty,
                string.Empty,
                new List<MissionEntryData>(),
                new List<MissionEntryData>(),
                new MissionBonusStatus());
        }

        List<MissionEntryData> skillMissions = gameManager.GetSkillMissions();
        List<MissionEntryData> routines = gameManager.GetRoutineMissions();
        MissionBonusStatus bonus = gameManager.GetSkillMissionBonusStatus();

        return new MissionPanelViewState(
            "Missions",
            $"Reset in: {gameManager.GetMissionResetCountdownLabel()}",
            $"{bonus.completedSelectedSkillMissionCount}/{Mathf.Max(5, bonus.selectedSkillMissionCount)} tracked missions completed",
            skillMissions,
            routines,
            bonus);
    }

    public List<SkillEntry> GetSkills()
    {
        return gameManager != null ? gameManager.GetSkills() : new List<SkillEntry>();
    }

    public string GetSkillChoiceLabel(SkillEntry skill)
    {
        if (skill == null)
        {
            return string.Empty;
        }

        if (gameManager == null)
        {
            return skill.name ?? string.Empty;
        }

        SkillProgressionViewData view = gameManager.GetSkillProgressionView(skill.id);
        return view == null
            ? skill.name
            : $"{skill.name}  Lv.{view.level}  {view.progressToNextLevelPercent:0.#}%";
    }

    public int GetRoutineCreationCost()
    {
        return gameManager != null ? gameManager.GetRoutineCreationCost() : 0;
    }

    public MissionPanelActionResult ToggleSkillTracking(string missionId, bool shouldSelect)
    {
        if (gameManager == null)
        {
            return new MissionPanelActionResult(false, "Mission system unavailable");
        }

        string message;
        bool success = shouldSelect
            ? gameManager.SelectMission(missionId, out message)
            : gameManager.UnselectMission(missionId, out message);

        return new MissionPanelActionResult(
            success,
            success ? (shouldSelect ? "Mission tracking enabled" : "Mission tracking removed") : message);
    }

    public MissionPanelActionResult ClaimSkillMission(string missionId)
    {
        if (gameManager == null)
        {
            return new MissionPanelActionResult(false, "Mission system unavailable");
        }

        gameManager.OnClaimMissionButton(missionId);
        return new MissionPanelActionResult(true, "Mission claimed");
    }

    public MissionPanelActionResult CompleteRoutine(string missionId)
    {
        if (gameManager == null)
        {
            return new MissionPanelActionResult(false, "Mission system unavailable");
        }

        string message;
        bool success = gameManager.CompleteRoutineMission(missionId, out message);
        return new MissionPanelActionResult(success, success ? "Routine completed" : message);
    }

    public MissionPanelActionResult ClaimBonus()
    {
        if (gameManager == null)
        {
            return new MissionPanelActionResult(false, "Mission system unavailable");
        }

        string message;
        bool success = gameManager.ClaimSkillMissionBonus(out message);
        return new MissionPanelActionResult(success, success ? "Bonus claimed" : message);
    }

    public MissionPanelActionResult CreateSkillMission(string skillId, int minutes)
    {
        if (gameManager == null)
        {
            return new MissionPanelActionResult(false, "Mission system unavailable");
        }

        string message;
        bool success = gameManager.CreateCustomSkillMission(skillId, minutes, out message);
        return new MissionPanelActionResult(success, success ? "Mission created" : message);
    }

    public MissionPanelActionResult CreateRoutineMission(string title, int coins, int mood, int energy, int skillSp, string rewardSkillId)
    {
        if (gameManager == null)
        {
            return new MissionPanelActionResult(false, "Mission system unavailable");
        }

        string message;
        bool success = gameManager.CreateRoutineMission(title, coins, mood, energy, skillSp, rewardSkillId, out message);
        return new MissionPanelActionResult(success, success ? "Mission created" : message);
    }
}
