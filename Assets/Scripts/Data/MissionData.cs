using System.Collections.Generic;

[System.Serializable]
public enum MissionDifficulty
{
    Easy,
    Medium,
    Hard
}

[System.Serializable]
public class MissionEntryData
{
    public string missionId;
    public string missionType;
    public MissionDifficulty difficulty = MissionDifficulty.Easy;
    public string skillMissionMode;
    public string title;
    public string skillId;
    public string targetSkillId;
    public string targetSkillName;
    public int currentProgress;
    public int targetProgress;
    public float requiredMinutes;
    public float progressMinutes;
    public int rewardCoins;
    public int rewardMood;
    public int rewardEnergy;
    public int rewardSkillSP;
    public float rewardSkillPercent;
    public string rewardSkillId;
    public bool isRoutine;
    public bool isCustom;
    public bool isManualRoutine;
    public bool autoClaimOnComplete;
    public bool isSelected;
    public bool hasSelectionState;
    public bool isCompleted;
    public bool isClaimed;
}

[System.Serializable]
public class MissionSkillPreferenceData
{
    public string skillId = "";
    public float score = 0f;
}

[System.Serializable]
public class MissionPersonalizationProfileData
{
    public float feedScore = 0f;
    public float workScore = 0f;
    public float focusScore = 0f;
    public float skillScore = 0f;
    public float recentFocusMinutes = 0f;
    public string lastDecayResetKey = "";
    public List<MissionSkillPreferenceData> skillPreferences = new List<MissionSkillPreferenceData>();
}

[System.Serializable]
public class MissionData
{
    public string lastDailyResetKey;
    public List<MissionEntryData> missions = new List<MissionEntryData>();
    public MissionPersonalizationProfileData personalizationProfile = new MissionPersonalizationProfileData();
    public bool skillBonusClaimed;
    public int customRoutineCreateCount;
}
