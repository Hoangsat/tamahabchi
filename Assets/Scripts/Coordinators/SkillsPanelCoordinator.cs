using System.Collections.Generic;

public readonly struct SkillsPanelSnapshot
{
    public SkillsPanelSnapshot(List<SkillEntry> skills, List<SkillProgressionViewData> skillViews, string selectedSkillId)
    {
        Skills = skills ?? new List<SkillEntry>();
        SkillViews = skillViews ?? new List<SkillProgressionViewData>();
        SelectedSkillId = selectedSkillId ?? string.Empty;
    }

    public List<SkillEntry> Skills { get; }
    public List<SkillProgressionViewData> SkillViews { get; }
    public string SelectedSkillId { get; }
}

public readonly struct SkillsPanelActionResult
{
    public SkillsPanelActionResult(bool success, string message, SkillEntry skill = null)
    {
        Success = success;
        Message = message ?? string.Empty;
        Skill = skill;
    }

    public bool Success { get; }
    public string Message { get; }
    public SkillEntry Skill { get; }
}

public readonly struct SkillsHeroState
{
    public SkillsHeroState(
        SkillEntry heroSkill,
        SkillProgressionViewData heroView,
        bool usingSelectedSkill,
        string heroLabel,
        string hudLabel,
        string heroNameText,
        string heroMetaText,
        string progressLabel,
        string hintText,
        string actionText)
    {
        HeroSkill = heroSkill;
        HeroView = heroView;
        UsingSelectedSkill = usingSelectedSkill;
        HeroLabel = heroLabel ?? string.Empty;
        HudLabel = hudLabel ?? string.Empty;
        HeroNameText = heroNameText ?? string.Empty;
        HeroMetaText = heroMetaText ?? string.Empty;
        ProgressLabel = progressLabel ?? string.Empty;
        HintText = hintText ?? string.Empty;
        ActionText = actionText ?? string.Empty;
    }

    public SkillEntry HeroSkill { get; }
    public SkillProgressionViewData HeroView { get; }
    public bool UsingSelectedSkill { get; }
    public string HeroLabel { get; }
    public string HudLabel { get; }
    public string HeroNameText { get; }
    public string HeroMetaText { get; }
    public string ProgressLabel { get; }
    public string HintText { get; }
    public string ActionText { get; }
}

public sealed class SkillsPanelCoordinator
{
    private readonly GameManager gameManager;

    public SkillsPanelCoordinator(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public SkillsPanelSnapshot GetSnapshot()
    {
        if (gameManager == null)
        {
            return new SkillsPanelSnapshot(new List<SkillEntry>(), new List<SkillProgressionViewData>(), string.Empty);
        }

        return new SkillsPanelSnapshot(
            gameManager.GetSkills(),
            gameManager.GetSkillProgressionViews(),
            gameManager.GetSelectedFocusSkill());
    }

    public SkillsHeroState GetHeroState(SkillsPanelSnapshot snapshot)
    {
        SkillEntry heroSkill = ResolveHeroSkill(snapshot.Skills, snapshot.SelectedSkillId, out bool usingSelectedSkill);
        SkillProgressionViewData heroView = heroSkill != null ? GetSkillProgressionView(heroSkill.id) : null;
        string selectedName = heroSkill != null ? heroSkill.name : "None";
        string heroLabel = heroSkill == null
            ? "No Skills Yet"
            : usingSelectedSkill
                ? "Current Focus"
                : "Top Skill";
        string hudLabel = heroView == null
            ? "Current Focus: " + selectedName
            : $"Current Focus: {selectedName} - Lv.{heroView.level}";
        string actionText = heroSkill == null
            ? "Create Skill"
            : usingSelectedSkill
                ? "Start Focus"
                : "Focus This";

        return new SkillsHeroState(
            heroSkill,
            heroView,
            usingSelectedSkill,
            heroLabel,
            hudLabel,
            heroSkill == null ? "Create your first skill" : heroSkill.name,
            BuildHeroMeta(heroView, usingSelectedSkill),
            BuildHeroProgressLabel(heroView),
            BuildHeroHint(heroView, usingSelectedSkill),
            actionText);
    }

    public bool HasDuplicateSkillName(string candidateName)
    {
        return gameManager != null && gameManager.HasSkillName(candidateName);
    }

    public List<SkillArchetypeDefinition> GetSelectableArchetypes()
    {
        return gameManager != null
            ? gameManager.GetSelectableSkillArchetypes()
            : new List<SkillArchetypeDefinition>(SkillArchetypeCatalog.GetPlayerSelectableDefinitions());
    }

    public SkillArchetypeDefinition GetArchetype(string archetypeId)
    {
        return gameManager != null
            ? gameManager.GetSkillArchetype(archetypeId)
            : SkillArchetypeCatalog.GetDefinition(archetypeId);
    }

    public SkillsPanelActionResult AddSkill(string candidateName, string archetypeId)
    {
        if (gameManager == null)
        {
            return new SkillsPanelActionResult(false, "GameManager missing");
        }

        if (gameManager.HasSkillName(candidateName))
        {
            return new SkillsPanelActionResult(false, "Skill already exists");
        }

        string normalizedArchetypeId = ResolveRequestedArchetypeId(archetypeId);
        if (!SkillArchetypeCatalog.IsSelectable(normalizedArchetypeId))
        {
            return new SkillsPanelActionResult(false, "Выберите тип навыка");
        }

        SkillEntry addedSkill = gameManager.AddSkillWithArchetype(candidateName, normalizedArchetypeId);
        if (addedSkill == null)
        {
            return new SkillsPanelActionResult(false, "Enter a valid name");
        }

        return new SkillsPanelActionResult(true, "Skill created", addedSkill);
    }

    public SkillsPanelActionResult ChangeSkillArchetype(string skillId, string archetypeId)
    {
        if (gameManager == null)
        {
            return new SkillsPanelActionResult(false, "GameManager missing");
        }

        string normalizedArchetypeId = ResolveRequestedArchetypeId(archetypeId);
        if (!SkillArchetypeCatalog.IsSelectable(normalizedArchetypeId))
        {
            return new SkillsPanelActionResult(false, "Выберите тип навыка");
        }

        return gameManager.ChangeSkillArchetype(skillId, normalizedArchetypeId)
            ? new SkillsPanelActionResult(true, "Тип навыка обновлён", gameManager.GetSkillById(skillId))
            : new SkillsPanelActionResult(false, "Не удалось обновить тип навыка");
    }

    public SkillsPanelActionResult SelectSkillForFocus(string skillId)
    {
        if (gameManager == null)
        {
            return new SkillsPanelActionResult(false, string.Empty);
        }

        if (!gameManager.SetSelectedFocusSkill(skillId))
        {
            return new SkillsPanelActionResult(false, string.Empty);
        }

        if (!gameManager.OpenFocusPanel(skillId))
        {
            return new SkillsPanelActionResult(true, "Focus skill selected");
        }

        return new SkillsPanelActionResult(true, string.Empty);
    }

    public SkillsPanelActionResult RemoveSkill(string skillId)
    {
        if (gameManager == null)
        {
            return new SkillsPanelActionResult(false, string.Empty);
        }

        return gameManager.RemoveSkill(skillId)
            ? new SkillsPanelActionResult(true, "Skill removed")
            : new SkillsPanelActionResult(false, "Only untrained skills can be removed");
    }

    public SkillEntry GetSkillById(string skillId)
    {
        return gameManager != null ? gameManager.GetSkillById(skillId) : null;
    }

    public SkillProgressionViewData GetSkillProgressionView(string skillId)
    {
        return gameManager != null ? gameManager.GetSkillProgressionView(skillId) : null;
    }

    public SkillsPanelActionResult StartHeroFocus(string heroSkillId)
    {
        if (gameManager == null)
        {
            return new SkillsPanelActionResult(false, string.Empty);
        }

        gameManager.SetSelectedFocusSkill(heroSkillId);
        if (!gameManager.OpenFocusPanel(heroSkillId))
        {
            return new SkillsPanelActionResult(true, "Focus is unavailable right now");
        }

        return new SkillsPanelActionResult(true, string.Empty);
    }

    private SkillEntry ResolveHeroSkill(List<SkillEntry> skills, string selectedSkillId, out bool usingSelectedSkill)
    {
        usingSelectedSkill = false;
        if (skills == null || skills.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < skills.Count; i++)
        {
            SkillEntry skill = skills[i];
            if (skill != null && skill.id == selectedSkillId)
            {
                usingSelectedSkill = true;
                return skill;
            }
        }

        SkillEntry bestSkill = null;
        for (int i = 0; i < skills.Count; i++)
        {
            SkillEntry candidate = skills[i];
            if (candidate == null)
            {
                continue;
            }

            if (bestSkill == null)
            {
                bestSkill = candidate;
                continue;
            }

            float candidateAxis = GetSkillProgressionView(candidate.id)?.axisPercent ?? SkillProgressionModel.GetAxisPercent(candidate.totalSP);
            float bestAxis = GetSkillProgressionView(bestSkill.id)?.axisPercent ?? SkillProgressionModel.GetAxisPercent(bestSkill.totalSP);

            if (candidateAxis > bestAxis)
            {
                bestSkill = candidate;
                continue;
            }

            if (UnityEngine.Mathf.Approximately(candidateAxis, bestAxis) && candidate.totalFocusMinutes > bestSkill.totalFocusMinutes)
            {
                bestSkill = candidate;
            }
        }

        return bestSkill ?? skills[0];
    }

    private static string BuildHeroMeta(SkillProgressionViewData heroView, bool usingSelectedSkill)
    {
        if (heroView == null)
        {
            return "Train a talent and turn it into your pet's signature skill.";
        }

        string role = usingSelectedSkill ? "Selected" : "Recommended";
        string minutes = heroView.totalFocusMinutes > 0 ? $" | {heroView.totalFocusMinutes}m logged" : " | Fresh track";
        string golden = heroView.isGolden ? " | Golden" : string.Empty;
        return $"{role} Focus | Axis {heroView.axisPercent:0.#}%{minutes}{golden}";
    }

    private static string BuildHeroProgressLabel(SkillProgressionViewData heroView)
    {
        if (heroView == null)
        {
            return "LEVEL 0\n0% to Lv.1";
        }

        if (heroView.isMaxed)
        {
            return $"LEVEL {heroView.level}\nMAXED";
        }

        return $"LEVEL {heroView.level}\n{heroView.progressToNextLevelPercent:0.#}% to Lv.{UnityEngine.Mathf.Min(heroView.level + 1, SkillProgressionModel.MaxLevel)}";
    }

    private static string BuildHeroHint(SkillProgressionViewData heroView, bool usingSelectedSkill)
    {
        if (heroView == null)
        {
            return "Create your first skill below, then launch a focused training run.";
        }

        if (heroView.isGolden)
        {
            return $"Axis 100% | {heroView.totalSP} total SP | Golden bonus is active.";
        }

        string progressSnapshot = $"{heroView.progressInLevel}/{heroView.requiredSPForNextLevel} SP in this level";
        return usingSelectedSkill
            ? $"{progressSnapshot} | Ready for your next focus session."
            : $"{progressSnapshot} | Strong candidate for your next focus run.";
    }

    private static string ResolveRequestedArchetypeId(string rawValue)
    {
        if (SkillArchetypeCatalog.IsSelectable(rawValue))
        {
            return SkillArchetypeCatalog.NormalizeArchetypeId(rawValue);
        }

        string resolvedFromLegacyIcon = SkillArchetypeCatalog.ResolveArchetypeIdFromLegacyIcon(rawValue);
        return SkillArchetypeCatalog.NormalizeArchetypeId(resolvedFromLegacyIcon);
    }
}
