using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MissionGenerationDebugSnapshot
{
    public string generationKey = string.Empty;
    public string resetKey = string.Empty;
    public string generatedAtUtc = string.Empty;
    public string generationReason = string.Empty;
    public List<string> difficultyPlan = new List<string>();
    public List<MissionGenerationStepDebugInfo> steps = new List<MissionGenerationStepDebugInfo>();
    public List<MissionCandidateDebugInfo> candidates = new List<MissionCandidateDebugInfo>();
    public List<MissionGenerationPickDebugInfo> picks = new List<MissionGenerationPickDebugInfo>();
    public List<MissionCompositionAttemptDebugInfo> compositionAttempts = new List<MissionCompositionAttemptDebugInfo>();
    public List<MissionCompositionRepairDebugInfo> repairs = new List<MissionCompositionRepairDebugInfo>();
    public List<MissionDebugMissionInfo> chosenBaseMissions = new List<MissionDebugMissionInfo>();
    public List<MissionDebugMissionInfo> finalMissions = new List<MissionDebugMissionInfo>();
    public List<string> fallbackPath = new List<string>();
    public MissionPersonalizationDebugSummary personalizationSummary = new MissionPersonalizationDebugSummary();
    public MissionDecayDebugSummary decaySummary = new MissionDecayDebugSummary();
    public string compactReport = string.Empty;

    public MissionGenerationDebugSnapshot Clone()
    {
        return JsonUtility.FromJson<MissionGenerationDebugSnapshot>(JsonUtility.ToJson(this));
    }
}

[Serializable]
public class MissionGenerationStepDebugInfo
{
    public string phase = string.Empty;
    public string detail = string.Empty;
}

[Serializable]
public class MissionCandidateDebugInfo
{
    public string phase = string.Empty;
    public int slotIndex = -1;
    public string candidateKey = string.Empty;
    public string missionType = string.Empty;
    public string category = string.Empty;
    public string difficulty = string.Empty;
    public string targetSkillId = string.Empty;
    public string targetSkillName = string.Empty;
    public float baseWeight = 1f;
    public float categoryMultiplier = 1f;
    public float skillMultiplier = 1f;
    public float contextMultiplier = 1f;
    public float postClampMultiplier = 1f;
    public float rawWeight = 1f;
    public float clampedWeight = 1f;
    public float personalizationMultiplier = 1f;
    public float finalWeight = 1f;
    public bool picked;
    public bool replaced;
    public string outcome = string.Empty;
    public string notes = string.Empty;
}

[Serializable]
public class MissionGenerationPickDebugInfo
{
    public int slotIndex = -1;
    public string slotDifficulty = string.Empty;
    public string candidateKey = string.Empty;
    public bool usedFallback;
    public string fallbackReason = string.Empty;
    public string outcome = string.Empty;
    public MissionDebugMissionInfo mission = new MissionDebugMissionInfo();
}

[Serializable]
public class MissionCompositionAttemptDebugInfo
{
    public string attemptLabel = string.Empty;
    public bool enforceUniqueSkill;
    public int maxFocusBucket;
    public bool requireNonFocus;
    public bool usedRelaxedFallbackProfile;
    public bool validAfterAttempt;
}

[Serializable]
public class MissionCompositionRepairDebugInfo
{
    public string attemptLabel = string.Empty;
    public string rule = string.Empty;
    public int replaceIndex = -1;
    public MissionDebugMissionInfo replacedMission = new MissionDebugMissionInfo();
    public MissionDebugMissionInfo replacementMission = new MissionDebugMissionInfo();
    public bool sameDifficulty;
    public bool usedRelaxedFallbackProfile;
    public string detail = string.Empty;
}

[Serializable]
public class MissionDebugMissionInfo
{
    public string missionId = string.Empty;
    public string title = string.Empty;
    public string missionType = string.Empty;
    public string category = string.Empty;
    public string difficulty = string.Empty;
    public string targetSkillId = string.Empty;
    public string targetSkillName = string.Empty;
    public string skillMissionMode = string.Empty;
    public string outcome = string.Empty;
}

[Serializable]
public class MissionPersonalizationDebugSummary
{
    public float feedScore;
    public float workScore;
    public float focusScore;
    public float skillScore;
    public float recentFocusMinutes;
    public bool decayAppliedSincePreviousCycle;
    public List<MissionSkillPreferenceDebugInfo> topSkillPreferences = new List<MissionSkillPreferenceDebugInfo>();
}

[Serializable]
public class MissionSkillPreferenceDebugInfo
{
    public string skillId = string.Empty;
    public string skillName = string.Empty;
    public float score;
}

[Serializable]
public class MissionDecayDebugSummary
{
    public bool resetTriggered;
    public bool decayApplied;
    public bool ignoredMissionPenaltyApplied;
    public string previousLastDecayResetKey = string.Empty;
    public string newLastDecayResetKey = string.Empty;
    public MissionPersonalizationDebugSummary beforeProfile = new MissionPersonalizationDebugSummary();
    public MissionPersonalizationDebugSummary afterProfile = new MissionPersonalizationDebugSummary();
    public List<MissionIgnoredMissionPenaltyDebugInfo> ignoredMissionPenalties = new List<MissionIgnoredMissionPenaltyDebugInfo>();
}

[Serializable]
public class MissionIgnoredMissionPenaltyDebugInfo
{
    public string missionId = string.Empty;
    public string title = string.Empty;
    public string category = string.Empty;
    public string targetSkillId = string.Empty;
    public float categoryPenalty;
    public float skillPenalty;
}
