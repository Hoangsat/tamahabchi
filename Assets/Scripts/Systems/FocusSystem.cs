using UnityEngine;

public class FocusSystem
{
    private float elapsedSeconds = 0f;
    private float duration = 0f;
    private FocusSessionState state = FocusSessionState.Idle;
    private string currentFocusSkillId = string.Empty;
    private FocusSessionCompletionData pendingCompletion;
    private float lastRealtimeMark = -1f;

    public bool IsRunning => state == FocusSessionState.Running;
    public bool IsPaused => state == FocusSessionState.Paused;
    public bool HasActiveSession => state == FocusSessionState.Running || state == FocusSessionState.Paused;
    public FocusSessionState State => state;

    public void StartFocus(float duration)
    {
        StartFocus(duration, string.Empty);
    }

    public void StartFocus(float duration, string skillId)
    {
        this.duration = Mathf.Max(0f, duration);
        elapsedSeconds = 0f;
        state = FocusSessionState.Running;
        currentFocusSkillId = NormalizeSkillId(skillId);
        pendingCompletion = null;
        lastRealtimeMark = Time.realtimeSinceStartup;
    }

    public bool Update(float deltaTime)
    {
        if (state != FocusSessionState.Running)
        {
            return false;
        }

        float now = Time.realtimeSinceStartup;
        float realtimeDelta = lastRealtimeMark >= 0f ? now - lastRealtimeMark : 0f;
        float appliedDelta = realtimeDelta > 0.0001f ? realtimeDelta : Mathf.Max(0f, deltaTime);
        elapsedSeconds = Mathf.Clamp(elapsedSeconds + appliedDelta, 0f, duration);
        lastRealtimeMark = now;

        if (elapsedSeconds >= duration)
        {
            CompleteCurrentSession(duration, true);
            return true;
        }

        return false;
    }

    public bool PauseFocus()
    {
        if (state != FocusSessionState.Running)
        {
            return false;
        }

        elapsedSeconds = GetElapsedTime();
        state = FocusSessionState.Paused;
        lastRealtimeMark = -1f;
        return true;
    }

    public bool ResumeFocus()
    {
        if (state != FocusSessionState.Paused)
        {
            return false;
        }

        state = FocusSessionState.Running;
        lastRealtimeMark = Time.realtimeSinceStartup;
        return true;
    }

    public bool CancelFocus()
    {
        if (!HasActiveSession)
        {
            return false;
        }

        elapsedSeconds = 0f;
        duration = 0f;
        state = FocusSessionState.Idle;
        currentFocusSkillId = string.Empty;
        pendingCompletion = null;
        lastRealtimeMark = -1f;
        return true;
    }

    public bool FinishFocusEarly()
    {
        if (!HasActiveSession)
        {
            return false;
        }

        CompleteCurrentSession(GetElapsedTime(), false);
        return true;
    }

    public float GetRemainingTime()
    {
        return Mathf.Max(0f, duration - GetElapsedTime());
    }

    public float GetElapsedTime()
    {
        if (state != FocusSessionState.Running)
        {
            return Mathf.Clamp(elapsedSeconds, 0f, duration);
        }

        float now = Time.realtimeSinceStartup;
        float realtimeDelta = lastRealtimeMark >= 0f ? now - lastRealtimeMark : 0f;
        float appliedDelta = realtimeDelta > 0.0001f ? realtimeDelta : 0f;
        return Mathf.Clamp(elapsedSeconds + appliedDelta, 0f, duration);
    }

    public float GetConfiguredDuration()
    {
        return Mathf.Max(0f, duration);
    }

    public FocusSessionSnapshot GetSnapshot()
    {
        return new FocusSessionSnapshot
        {
            state = state,
            skillId = currentFocusSkillId,
            configuredDurationSeconds = Mathf.Max(0f, duration),
            elapsedSeconds = GetElapsedTime(),
            remainingSeconds = GetRemainingTime()
        };
    }

    public bool TryConsumeCompletedSession(out FocusSessionCompletionData completionData)
    {
        completionData = pendingCompletion;
        pendingCompletion = null;
        return completionData != null;
    }

    public FocusSessionSaveData CreateSaveData(string savedAtUtc)
    {
        if (!HasActiveSession)
        {
            return null;
        }

        return new FocusSessionSaveData
        {
            state = state,
            skillId = currentFocusSkillId,
            configuredDurationSeconds = Mathf.Max(0f, duration),
            elapsedSeconds = GetElapsedTime(),
            savedAtUtc = string.IsNullOrWhiteSpace(savedAtUtc) ? string.Empty : savedAtUtc.Trim()
        };
    }

    public bool RestoreSession(FocusSessionSaveData saveData, double offlineElapsedSeconds, out bool completedWhileOffline)
    {
        completedWhileOffline = false;
        if (HasActiveSession)
        {
            CancelFocus();
        }

        if (saveData == null || !saveData.HasSessionData() || saveData.configuredDurationSeconds <= 0f)
        {
            return false;
        }

        duration = Mathf.Max(0f, saveData.configuredDurationSeconds);
        currentFocusSkillId = NormalizeSkillId(saveData.skillId);
        elapsedSeconds = Mathf.Clamp(saveData.elapsedSeconds, 0f, duration);
        pendingCompletion = null;

        if (saveData.state == FocusSessionState.Running)
        {
            elapsedSeconds = Mathf.Clamp(elapsedSeconds + Mathf.Max(0f, (float)offlineElapsedSeconds), 0f, duration);
        }

        if (elapsedSeconds >= duration)
        {
            state = FocusSessionState.Running;
            CompleteCurrentSession(duration, true);
            completedWhileOffline = true;
            return true;
        }

        state = saveData.state == FocusSessionState.Paused ? FocusSessionState.Paused : FocusSessionState.Running;
        lastRealtimeMark = state == FocusSessionState.Running ? Time.realtimeSinceStartup : -1f;
        return true;
    }

    public FocusRewardData BuildReward(
        float plannedDurationSeconds,
        float actualDurationSeconds,
        int plannedCoinsReward,
        int plannedXpReward,
        float plannedEnergyReward,
        float plannedMoodReward)
    {
        float safePlannedDuration = Mathf.Max(0f, plannedDurationSeconds);
        float safeActualDuration = Mathf.Clamp(
            actualDurationSeconds,
            0f,
            safePlannedDuration > 0f ? safePlannedDuration : Mathf.Max(0f, actualDurationSeconds));
        float completionRatio = safePlannedDuration > 0.0001f ? Mathf.Clamp01(safeActualDuration / safePlannedDuration) : 0f;

        return new FocusRewardData
        {
            completionRatio = completionRatio,
            coins = Mathf.Max(0, Mathf.RoundToInt(plannedCoinsReward * completionRatio)),
            xp = Mathf.Max(0, Mathf.RoundToInt(plannedXpReward * completionRatio)),
            energy = Mathf.Max(0f, plannedEnergyReward * completionRatio),
            mood = Mathf.Max(0f, plannedMoodReward * completionRatio)
        };
    }

    private void CompleteCurrentSession(float actualDurationSeconds, bool completedNaturally)
    {
        pendingCompletion = new FocusSessionCompletionData
        {
            skillId = currentFocusSkillId,
            plannedDurationSeconds = Mathf.Max(0f, duration),
            actualDurationSeconds = Mathf.Clamp(actualDurationSeconds, 0f, duration),
            completedNaturally = completedNaturally,
            completedEarly = !completedNaturally
        };

        elapsedSeconds = 0f;
        duration = 0f;
        state = FocusSessionState.Idle;
        currentFocusSkillId = string.Empty;
        lastRealtimeMark = -1f;
    }

    private string NormalizeSkillId(string skillId)
    {
        return string.IsNullOrWhiteSpace(skillId) ? string.Empty : skillId.Trim();
    }
}
