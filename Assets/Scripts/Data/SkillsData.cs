using System.Collections.Generic;

[System.Serializable]
public class SkillEntry
{
    public string id = "";
    public string name = "";
    public string icon = "";
    public float percent = 0f;
    public bool isGolden = false;
    public float bonusExpMultiplier = 0f;
    public int totalFocusMinutes = 0;
    public string lastFocusDate = "";
}

[System.Serializable]
public class SkillsData
{
    public List<SkillEntry> skills = new List<SkillEntry>();
}
