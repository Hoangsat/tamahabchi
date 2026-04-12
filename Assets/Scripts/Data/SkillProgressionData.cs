using System;

[Serializable]
public class SkillProgressionViewData
{
    public string id = string.Empty;
    public string name = string.Empty;
    public string icon = string.Empty;
    public string archetypeId = string.Empty;
    public int totalSP = 0;
    public int decayDebtSP = 0;
    public int effectiveSP = 0;
    public int level = 0;
    public int progressInLevel = 0;
    public int requiredSPForNextLevel = 0;
    public float progressInLevel01 = 0f;
    public float progressToNextLevelPercent = 0f;
    public float axisPercent = 0f;
    public float axisFill01 = 0f;
    public bool isGolden = false;
    public bool isMaxed = false;
    public int totalFocusMinutes = 0;
    public string lastFocusDate = string.Empty;
}
