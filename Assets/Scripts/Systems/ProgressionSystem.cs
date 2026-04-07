public class ProgressionSystem
{
    private ProgressionData progressionData;
    private int xpToNextLevel;
    private int buyUnlockLevel;

    public ProgressionSystem(ProgressionData progressionData, int xpToNextLevel, int buyUnlockLevel)
    {
        this.progressionData = progressionData;
        this.xpToNextLevel = xpToNextLevel;
        this.buyUnlockLevel = buyUnlockLevel;
    }

    public int GetLevel()
    {
        return progressionData.level;
    }

    public int GetXp()
    {
        return progressionData.xp;
    }

    public void AddXp(int amount)
    {
        if (amount <= 0) return;

        progressionData.xp += amount;

        while (progressionData.xp >= GetXpRequiredForNextLevel())
        {
            progressionData.xp -= GetXpRequiredForNextLevel();
            progressionData.level++;
        }
    }

    public int GetXpRequiredForNextLevel()
    {
        return xpToNextLevel; // Preserved original flat 10 XP per level logic
    }

    public int GetWorkReward(int baseReward)
    {
        return baseReward + (progressionData.level - 1);
    }

    public int GetFocusReward(int baseReward)
    {
        return baseReward + (progressionData.level - 1) * 2;
    }

    public bool IsBuyUnlocked()
    {
        return progressionData.level >= buyUnlockLevel;
    }
}
