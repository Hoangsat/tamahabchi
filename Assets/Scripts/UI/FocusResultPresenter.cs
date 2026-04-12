using UnityEngine;

public sealed class FocusResultViewData
{
    public string Title;
    public string Subtitle;
    public string Skill;
    public string Progress;
    public string Reward;
    public string Pet;
    public Color TitleColor;
    public Color RewardColor;
    public Color PetColor;
}

public static class FocusResultPresenter
{
    public static FocusResultViewData Build(FocusSessionResultData result)
    {
        FocusResultViewData viewData = new FocusResultViewData
        {
            Title = string.Empty,
            Subtitle = string.Empty,
            Skill = string.Empty,
            Progress = string.Empty,
            Reward = string.Empty,
            Pet = string.Empty,
            TitleColor = new Color(0.96f, 0.98f, 1f, 1f),
            RewardColor = new Color(1f, 0.86f, 0.45f, 1f),
            PetColor = new Color(0.86f, 0.91f, 0.97f, 0.96f)
        };

        if (result == null)
        {
            return viewData;
        }

        int plannedMinutes = Mathf.Max(0, Mathf.RoundToInt(result.plannedDurationSeconds / 60f));
        int actualMinutes = Mathf.Max(0, Mathf.RoundToInt(result.actualDurationSeconds / 60f));
        bool completedEarly = result.outcome == FocusSessionOutcome.CompletedEarly;
        bool levelUp = result.newLevel > result.previousLevel;

        viewData.Title = completedEarly ? "Completed Early" : "Completed";
        viewData.TitleColor = completedEarly
            ? new Color(1f, 0.83f, 0.5f, 1f)
            : new Color(0.63f, 0.92f, 0.7f, 1f);

        if (result.becameGolden || result.isGolden)
        {
            viewData.Title = "Skill Maxed";
            viewData.TitleColor = new Color(1f, 0.88f, 0.42f, 1f);
        }
        else if (levelUp)
        {
            viewData.Title = completedEarly ? "Level Up" : "Session Complete";
        }

        viewData.Subtitle = completedEarly
            ? $"You stayed locked in for {actualMinutes} of {plannedMinutes} min."
            : $"You completed the full {plannedMinutes} min session.";

        viewData.Skill = string.Format("{0}{1}",
            string.IsNullOrEmpty(result.skillIcon) ? string.Empty : result.skillIcon + " ",
            string.IsNullOrEmpty(result.skillName) ? "Unknown Skill" : result.skillName);

        string levelText = levelUp
            ? string.Format("Lv.{0} -> Lv.{1}", result.previousLevel, result.newLevel)
            : string.Format("Lv.{0}", result.newLevel);
        viewData.Progress = string.Format(
            "+{0} SP  |  {1}  |  Axis {2:0.#}% -> {3:0.#}%",
            result.skillSpReward,
            levelText,
            result.previousAxisPercent,
            result.newAxisPercent);

        string rewardLine = string.Format("Coins: +{0}", result.coinsReward);
        if (result.becameGolden)
        {
            rewardLine += "\nGolden state unlocked.";
        }
        else if (levelUp)
        {
            rewardLine += "\nLevel band advanced.";
        }

        viewData.Reward = rewardLine;

        string petLine = "Pet: " + (string.IsNullOrEmpty(result.petReaction) ? "Steady." : result.petReaction);
        if (result.lowEnergyPenaltyApplied)
        {
            petLine += "\nLow energy penalty applied during reward calculation.";
            viewData.PetColor = new Color(1f, 0.78f, 0.58f, 1f);
        }
        else
        {
            petLine += "\nFocus start cost: -1 mood.";
        }

        viewData.Pet = petLine;
        return viewData;
    }
}
