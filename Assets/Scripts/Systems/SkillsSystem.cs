using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SkillProgressResult
{
    public bool success;
    public string skillId = string.Empty;
    public int deltaSP;
    public int previousTotalSP;
    public int newTotalSP;
    public int previousLevel;
    public int newLevel;
    public float previousAxisPercent;
    public float newAxisPercent;
    public float previousProgressInLevel01;
    public float newProgressInLevel01;
    public bool leveledUp;
    public int levelsGained;
    public bool becameGolden;
    public bool isGolden;
}

public class SkillsSystem
{
    private const int MaxNameLength = 15;

    private SkillsData skillsData;

    public void Init(SkillsData data)
    {
        skillsData = data ?? new SkillsData();
        EnsureData();
    }

    public List<SkillEntry> GetSkills()
    {
        EnsureData();
        return new List<SkillEntry>(skillsData.skills);
    }

    public List<SkillProgressionViewData> GetSkillProgressionViews()
    {
        EnsureData();
        List<SkillProgressionViewData> views = new List<SkillProgressionViewData>(skillsData.skills.Count);
        for (int i = 0; i < skillsData.skills.Count; i++)
        {
            SkillEntry skill = skillsData.skills[i];
            if (skill == null)
            {
                continue;
            }

            views.Add(BuildProgressionView(skill));
        }

        return views;
    }

    public SkillProgressionViewData GetSkillProgressionView(string id)
    {
        SkillEntry skill = GetSkillById(id);
        return skill != null ? BuildProgressionView(skill) : null;
    }

    public SkillEntry GetSkillById(string id)
    {
        EnsureData();

        string normalizedId = NormalizeId(id);
        if (string.IsNullOrEmpty(normalizedId))
        {
            return null;
        }

        return skillsData.skills.Find(skill => skill != null && skill.id == normalizedId);
    }

    public bool CanAddSkill()
    {
        EnsureData();
        return true;
    }

    public bool HasSkillName(string name, string excludedSkillId = "")
    {
        EnsureData();

        string normalizedName = NormalizeName(name);
        if (string.IsNullOrEmpty(normalizedName))
        {
            return false;
        }

        string excludedId = NormalizeId(excludedSkillId);
        for (int i = 0; i < skillsData.skills.Count; i++)
        {
            SkillEntry skill = skillsData.skills[i];
            if (skill == null || skill.id == excludedId)
            {
                continue;
            }

            if (string.Equals(NormalizeName(skill.name), normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public SkillEntry AddSkill(string name, string icon)
    {
        string archetypeId = SkillArchetypeCatalog.ResolveArchetypeIdFromLegacyIcon(icon);
        return AddSkillWithArchetype(name, archetypeId);
    }

    public SkillEntry AddSkillWithArchetype(string name, string archetypeId)
    {
        EnsureData();

        string normalizedName = NormalizeName(name);
        if (string.IsNullOrEmpty(normalizedName) || !CanAddSkill() || HasSkillName(normalizedName))
        {
            return null;
        }

        string normalizedArchetypeId = SkillArchetypeCatalog.NormalizeArchetypeId(archetypeId);

        SkillEntry skill = new SkillEntry
        {
            id = "skill_" + Guid.NewGuid().ToString("N"),
            name = normalizedName,
            icon = SkillArchetypeCatalog.GetCanonicalIcon(normalizedArchetypeId),
            archetypeId = normalizedArchetypeId,
            totalSP = 0,
            decayDebtSP = 0,
            percent = 0f,
            isGolden = false,
            bonusExpMultiplier = 0f,
            totalFocusMinutes = 0,
            lastFocusDate = string.Empty
        };

        skillsData.skills.Add(skill);
        return skill;
    }

    public bool ChangeSkillArchetype(string skillId, string archetypeId)
    {
        SkillEntry skill = GetSkillById(skillId);
        if (skill == null)
        {
            return false;
        }

        string normalizedArchetypeId = SkillArchetypeCatalog.NormalizeArchetypeId(archetypeId);
        if (!SkillArchetypeCatalog.IsSelectable(normalizedArchetypeId))
        {
            return false;
        }

        skill.archetypeId = normalizedArchetypeId;
        skill.icon = SkillArchetypeCatalog.GetCanonicalIcon(normalizedArchetypeId);
        return true;
    }

    public bool CanRemoveSkill(string id)
    {
        SkillEntry skill = GetSkillById(id);
        return skill != null && skill.totalSP <= 0;
    }

    public bool RemoveSkill(string id)
    {
        SkillEntry skill = GetSkillById(id);
        if (skill == null || skill.totalSP > 0)
        {
            return false;
        }

        return skillsData.skills.Remove(skill);
    }

    public SkillProgressResult ApplySkillPoints(string id, int amount, float completedDurationSeconds, string localFocusDate, float goldenBonusIncrement)
    {
        SkillProgressResult result = new SkillProgressResult
        {
            skillId = NormalizeId(id)
        };

        SkillEntry skill = GetSkillById(id);
        if (skill == null || amount <= 0)
        {
            return result;
        }

        SkillProgressionViewData before = BuildProgressionView(skill);
        int previousTotalSP = skill.totalSP;
        int newTotalSP = SkillProgressionModel.ClampTotalSP(previousTotalSP + amount);

        skill.totalSP = newTotalSP;
        skill.totalFocusMinutes += Mathf.Max(0, Mathf.CeilToInt(completedDurationSeconds / 60f));
        skill.lastFocusDate = localFocusDate ?? string.Empty;

        bool becameGolden = !skill.isGolden && SkillProgressionModel.IsMaxed(newTotalSP);
        if (becameGolden)
        {
            skill.isGolden = true;
            skill.bonusExpMultiplier = Mathf.Max(skill.bonusExpMultiplier, goldenBonusIncrement);
        }
        else
        {
            skill.isGolden = SkillProgressionModel.IsMaxed(newTotalSP);
        }

        SkillProgressionViewData after = BuildProgressionView(skill);
        result.success = true;
        result.deltaSP = newTotalSP - previousTotalSP;
        result.previousTotalSP = previousTotalSP;
        result.newTotalSP = newTotalSP;
        result.previousLevel = before.level;
        result.newLevel = after.level;
        result.previousAxisPercent = before.axisPercent;
        result.newAxisPercent = after.axisPercent;
        result.previousProgressInLevel01 = before.progressInLevel01;
        result.newProgressInLevel01 = after.progressInLevel01;
        result.leveledUp = after.level > before.level;
        result.levelsGained = Mathf.Max(0, after.level - before.level);
        result.becameGolden = becameGolden;
        result.isGolden = after.isGolden;
        return result;
    }

    public int CalculateSkillPointsFromFocusDuration(float durationSeconds)
    {
        if (durationSeconds <= 0f)
        {
            return 0;
        }

        return Mathf.Max(0, Mathf.FloorToInt(durationSeconds / 60f));
    }

    public int ApplyFocusXpBonus(string skillId, int baseXp)
    {
        if (baseXp <= 0)
        {
            return 0;
        }

        SkillEntry skill = GetSkillById(skillId);
        if (skill == null || !skill.isGolden || skill.bonusExpMultiplier <= 0f)
        {
            return baseXp;
        }

        return Mathf.CeilToInt(baseXp * (1f + skill.bonusExpMultiplier));
    }

    public float GetPetLevelMultiplier(int petLevel, float skillLevelMultiplierStep)
    {
        int normalizedLevel = Mathf.Max(1, petLevel);
        return 1f + (normalizedLevel - 1) * Mathf.Max(0f, skillLevelMultiplierStep);
    }

    public bool IsGolden(string skillId)
    {
        SkillEntry skill = GetSkillById(skillId);
        return skill != null && skill.isGolden;
    }

    public List<SkillEntry> GetSkillsOrderedByAxisPercent()
    {
        EnsureData();
        List<SkillEntry> ordered = new List<SkillEntry>(skillsData.skills);
        ordered.Sort(CompareByAxisPercent);
        return ordered;
    }

    private SkillProgressionViewData BuildProgressionView(SkillEntry skill)
    {
        int totalSP = SkillProgressionModel.ClampTotalSP(skill != null ? skill.totalSP : 0);
        int decayDebtSP = Mathf.Clamp(skill != null ? skill.decayDebtSP : 0, 0, totalSP);
        int effectiveSP = GetEffectiveSP(totalSP, decayDebtSP);
        int level = SkillProgressionModel.GetLevel(totalSP);
        int requiredSPForNextLevel = SkillProgressionModel.GetRequiredSPForNextLevel(level);
        float progressInLevel01 = SkillProgressionModel.GetProgressInLevel01(totalSP);
        float axisPercent = SkillProgressionModel.GetAxisPercent(effectiveSP);
        bool isMaxed = SkillProgressionModel.IsMaxed(totalSP);

        return new SkillProgressionViewData
        {
            id = skill != null ? skill.id ?? string.Empty : string.Empty,
            name = skill != null ? skill.name ?? string.Empty : string.Empty,
            icon = skill != null ? skill.icon ?? string.Empty : string.Empty,
            archetypeId = skill != null ? skill.archetypeId ?? string.Empty : string.Empty,
            totalSP = totalSP,
            decayDebtSP = decayDebtSP,
            effectiveSP = effectiveSP,
            level = level,
            progressInLevel = isMaxed ? requiredSPForNextLevel : SkillProgressionModel.GetProgressInLevel(totalSP),
            requiredSPForNextLevel = requiredSPForNextLevel,
            progressInLevel01 = progressInLevel01,
            progressToNextLevelPercent = isMaxed ? 100f : progressInLevel01 * 100f,
            axisPercent = axisPercent,
            axisFill01 = axisPercent / 100f,
            isGolden = skill != null && skill.isGolden,
            isMaxed = isMaxed,
            totalFocusMinutes = skill != null ? Mathf.Max(0, skill.totalFocusMinutes) : 0,
            lastFocusDate = skill != null ? skill.lastFocusDate ?? string.Empty : string.Empty
        };
    }

    private int CompareByAxisPercent(SkillEntry left, SkillEntry right)
    {
        float leftAxis = left != null ? SkillProgressionModel.GetAxisPercent(GetEffectiveSP(left)) : 0f;
        float rightAxis = right != null ? SkillProgressionModel.GetAxisPercent(GetEffectiveSP(right)) : 0f;
        int axisComparison = rightAxis.CompareTo(leftAxis);
        if (axisComparison != 0)
        {
            return axisComparison;
        }

        int leftEffectiveSP = left != null ? GetEffectiveSP(left) : 0;
        int rightEffectiveSP = right != null ? GetEffectiveSP(right) : 0;
        int effectiveSpComparison = rightEffectiveSP.CompareTo(leftEffectiveSP);
        if (effectiveSpComparison != 0)
        {
            return effectiveSpComparison;
        }

        string leftDate = left != null ? left.lastFocusDate ?? string.Empty : string.Empty;
        string rightDate = right != null ? right.lastFocusDate ?? string.Empty : string.Empty;
        return string.Compare(rightDate, leftDate, StringComparison.Ordinal);
    }

    public int GetEffectiveSP(string id)
    {
        SkillEntry skill = GetSkillById(id);
        return GetEffectiveSP(skill);
    }

    public int GetEffectiveSP(SkillEntry skill)
    {
        return skill == null
            ? 0
            : GetEffectiveSP(SkillProgressionModel.ClampTotalSP(skill.totalSP), Mathf.Clamp(skill.decayDebtSP, 0, SkillProgressionModel.ClampTotalSP(skill.totalSP)));
    }

    public int GetEffectiveSP(int totalSP, int decayDebtSP)
    {
        return Mathf.Max(0, SkillProgressionModel.ClampTotalSP(totalSP) - Mathf.Clamp(decayDebtSP, 0, SkillProgressionModel.ClampTotalSP(totalSP)));
    }

    private void EnsureData()
    {
        if (skillsData == null)
        {
            skillsData = new SkillsData();
        }

        if (skillsData.skills == null)
        {
            skillsData.skills = new List<SkillEntry>();
        }

        for (int i = 0; i < skillsData.skills.Count; i++)
        {
            SkillEntry skill = skillsData.skills[i];
            if (skill == null)
            {
                skillsData.skills[i] = new SkillEntry
                {
                    id = "skill_" + Guid.NewGuid().ToString("N")
                };
                skill = skillsData.skills[i];
            }

            skill.id = string.IsNullOrWhiteSpace(skill.id) ? "skill_" + Guid.NewGuid().ToString("N") : skill.id.Trim();
            skill.name = skill.name ?? string.Empty;
            if (string.IsNullOrWhiteSpace(skill.archetypeId))
            {
                skill.archetypeId = SkillArchetypeCatalog.ResolveArchetypeIdFromLegacyIcon(skill.icon);
            }

            skill.archetypeId = SkillArchetypeCatalog.NormalizeArchetypeId(skill.archetypeId);
            skill.icon = SkillArchetypeCatalog.GetCanonicalIcon(skill.archetypeId);

            if (skill.totalSP <= 0 && skill.percent > 0f)
            {
                skill.totalSP = SkillProgressionModel.GetTotalSPForAxisPercent(skill.percent);
            }

            skill.totalSP = SkillProgressionModel.ClampTotalSP(skill.totalSP);
            skill.decayDebtSP = Mathf.Clamp(skill.decayDebtSP, 0, skill.totalSP);
            skill.percent = 0f;
            skill.isGolden = SkillProgressionModel.IsMaxed(skill.totalSP);
            if (skill.bonusExpMultiplier < 0f)
            {
                skill.bonusExpMultiplier = 0f;
            }

            if (skill.totalFocusMinutes < 0)
            {
                skill.totalFocusMinutes = 0;
            }

            skill.lastFocusDate = skill.lastFocusDate ?? string.Empty;
        }
    }

    private string NormalizeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
    }

    private string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        string trimmed = name.Trim();
        return trimmed.Length <= MaxNameLength ? trimmed : trimmed.Substring(0, MaxNameLength);
    }
}
