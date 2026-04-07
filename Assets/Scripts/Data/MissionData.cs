using System.Collections.Generic;

[System.Serializable]
public class MissionEntryData
{
    public string missionId;
    public int currentProgress;
    public int targetProgress;
    public int rewardCoins;
    public bool isCompleted;
    public bool isClaimed;
}

[System.Serializable]
public class MissionData
{
    public List<MissionEntryData> missions = new List<MissionEntryData>();
}
