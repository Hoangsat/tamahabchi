using UnityEngine;

public readonly struct MissionPanelBonusCardViewData
{
    public MissionPanelBonusCardViewData(string titleText, string progressText, Color progressColor, string claimButtonText, bool canClaim)
    {
        TitleText = titleText ?? string.Empty;
        ProgressText = progressText ?? string.Empty;
        ProgressColor = progressColor;
        ClaimButtonText = claimButtonText ?? string.Empty;
        CanClaim = canClaim;
    }

    public string TitleText { get; }
    public string ProgressText { get; }
    public Color ProgressColor { get; }
    public string ClaimButtonText { get; }
    public bool CanClaim { get; }
}

public readonly struct MissionPanelPopupDraftViewData
{
    public MissionPanelPopupDraftViewData(string titleText, string statusText, Color statusColor)
    {
        TitleText = titleText ?? string.Empty;
        StatusText = statusText ?? string.Empty;
        StatusColor = statusColor;
    }

    public string TitleText { get; }
    public string StatusText { get; }
    public Color StatusColor { get; }
}

public static class MissionPanelPresenter
{
    private static readonly Color BonusReadyColor = new Color(0.63f, 0.96f, 0.72f, 1f);
    private static readonly Color BonusDefaultColor = new Color(0.9f, 0.94f, 1f, 0.9f);
    private static readonly Color PopupHintColor = new Color(0.84f, 0.9f, 0.98f, 0.9f);
    private static readonly Color PopupReadyColor = new Color(0.67f, 0.95f, 0.75f, 1f);

    public static MissionPanelBonusCardViewData BuildBonusCard(MissionBonusStatus bonus)
    {
        bonus ??= new MissionBonusStatus();

        string title = bonus.isClaimed
            ? "5/5 Skill Mission Bonus Claimed"
            : bonus.isReady
                ? "5/5 Skill Mission Bonus Ready"
                : "5/5 Skill Mission Bonus";

        string progress = bonus.isClaimed
            ? "Reward already collected for this reset window."
            : bonus.isReady
                ? "All tracked missions are complete. Claim the bonus now."
                : string.Format("{0}/5 completed", bonus.completedSelectedSkillMissionCount);

        return new MissionPanelBonusCardViewData(
            title,
            progress,
            bonus.isReady ? BonusReadyColor : BonusDefaultColor,
            bonus.isClaimed ? "Claimed" : "Claim Bonus",
            bonus.isReady && !bonus.isClaimed);
    }

    public static string BuildRoutineCostText(int cost)
    {
        return cost > 0 ? $"Routine cost: {cost} coins" : "Routine cost: free";
    }

    public static string GetEmptyStateText()
    {
        return "No missions available";
    }

    public static string GetRerollStatusText()
    {
        return "Reroll stays out of scope for this milestone";
    }

    public static MissionPanelPopupDraftViewData BuildSkillMissionDraft(string skillLabel, int minutes)
    {
        bool hasSkill = !string.IsNullOrWhiteSpace(skillLabel);
        string title = "Create Skill Mission";
        string status = hasSkill
            ? string.Format("Draft: {0} for {1} min.", skillLabel, minutes)
            : "Pick a skill to generate a focus mission draft.";

        return new MissionPanelPopupDraftViewData(
            title,
            status,
            hasSkill ? PopupReadyColor : PopupHintColor);
    }

    public static MissionPanelPopupDraftViewData BuildRoutineDraft(string title, int coins, int mood, int energy, int skillSp, string rewardSkillName, int cost)
    {
        string cleanTitle = string.IsNullOrWhiteSpace(title) ? "Untitled routine" : title.Trim();
        string rewardSkill = string.IsNullOrWhiteSpace(rewardSkillName) ? "no skill target" : rewardSkillName.Trim();
        string status = string.Format(
            "Draft: {0} | Rewards {1} coins, {2} mood, {3} care, {4} SP to {5} | Cost {6} coins.",
            cleanTitle,
            coins,
            mood,
            energy,
            skillSp,
            rewardSkill,
            cost);

        return new MissionPanelPopupDraftViewData(
            "Create Routine",
            status,
            PopupHintColor);
    }
}
