using System;
using UnityEngine;

[Serializable]
public enum FocusSessionState
{
    Idle,
    Running,
    Paused
}

[Serializable]
public enum FocusSessionOutcome
{
    Completed,
    CompletedEarly,
    Cancelled
}

[Serializable]
public class FocusSessionSnapshot
{
    public FocusSessionState state = FocusSessionState.Idle;
    public string skillId = string.Empty;
    public float configuredDurationSeconds = 0f;
    public float elapsedSeconds = 0f;
    public float remainingSeconds = 0f;

    public bool HasActiveSession()
    {
        return state == FocusSessionState.Running || state == FocusSessionState.Paused;
    }

    public FocusSessionSnapshot Clone()
    {
        return JsonUtility.FromJson<FocusSessionSnapshot>(JsonUtility.ToJson(this));
    }
}

[Serializable]
public class FocusRewardData
{
    public float completionRatio = 0f;
    public int coins = 0;
    public int xp = 0;
    public float energy = 0f;
    public float mood = 0f;

    public FocusRewardData Clone()
    {
        return JsonUtility.FromJson<FocusRewardData>(JsonUtility.ToJson(this));
    }
}

[Serializable]
public class FocusSessionSaveData
{
    public FocusSessionState state = FocusSessionState.Idle;
    public string skillId = string.Empty;
    public float configuredDurationSeconds = 0f;
    public float elapsedSeconds = 0f;
    public string savedAtUtc = string.Empty;

    public bool HasSessionData()
    {
        return configuredDurationSeconds > 0f && !string.IsNullOrWhiteSpace(skillId);
    }

    public FocusSessionSaveData Clone()
    {
        return JsonUtility.FromJson<FocusSessionSaveData>(JsonUtility.ToJson(this));
    }
}

[Serializable]
public class FocusStateData
{
    public string selectedSkillId = string.Empty;
    public FocusSessionSaveData activeSession;
    public FocusSessionResultData lastResult;

    public FocusStateData Clone()
    {
        return JsonUtility.FromJson<FocusStateData>(JsonUtility.ToJson(this));
    }
}

[Serializable]
public class FocusSessionCompletionData
{
    public string skillId = string.Empty;
    public float plannedDurationSeconds = 0f;
    public float actualDurationSeconds = 0f;
    public bool completedNaturally = false;
    public bool completedEarly = false;
}

[Serializable]
public class FocusSessionResultData
{
    public FocusSessionOutcome outcome = FocusSessionOutcome.Completed;
    public string skillId = string.Empty;
    public string skillName = string.Empty;
    public string skillIcon = string.Empty;
    public float plannedDurationSeconds = 0f;
    public float actualDurationSeconds = 0f;
    public int skillSpReward = 0;
    public int previousTotalSP = 0;
    public int newTotalSP = 0;
    public int previousLevel = 0;
    public int newLevel = 0;
    public float previousAxisPercent = 0f;
    public float newAxisPercent = 0f;
    public float previousProgressInLevel01 = 0f;
    public float newProgressInLevel01 = 0f;
    public float previousPercent = 0f;
    public float newPercent = 0f;
    public float deltaProgress = 0f;
    public int coinsReward = 0;
    public int xpReward = 0;
    public float energyReward = 0f;
    public float moodReward = 0f;
    public float energyBefore = 0f;
    public float energyAfter = 0f;
    public string petReaction = string.Empty;
    public bool lowEnergyPenaltyApplied = false;
    public bool becameGolden = false;
    public bool isGolden = false;

    public FocusSessionResultData Clone()
    {
        return JsonUtility.FromJson<FocusSessionResultData>(JsonUtility.ToJson(this));
    }
}
