using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class SkillProgressResult
{
    public bool success;
    public string skillId = string.Empty;
    public float deltaApplied;
    public float previousPercent;
    public float newPercent;
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
        EnsureData();

        string normalizedName = NormalizeName(name);
        if (string.IsNullOrEmpty(normalizedName) || !CanAddSkill() || HasSkillName(normalizedName))
        {
            return null;
        }

        SkillEntry skill = new SkillEntry
        {
            id = "skill_" + Guid.NewGuid().ToString("N"),
            name = normalizedName,
            icon = (icon ?? string.Empty).Trim(),
            percent = 0f,
            isGolden = false,
            bonusExpMultiplier = 0f,
            totalFocusMinutes = 0,
            lastFocusDate = string.Empty
        };

        skillsData.skills.Add(skill);
        return skill;
    }

    public bool CanRemoveSkill(string id)
    {
        SkillEntry skill = GetSkillById(id);
        return skill != null && skill.percent <= 0f;
    }

    public bool RemoveSkill(string id)
    {
        SkillEntry skill = GetSkillById(id);
        if (skill == null || skill.percent > 0f)
        {
            return false;
        }

        return skillsData.skills.Remove(skill);
    }

    public SkillProgressResult ApplyFocusProgress(string id, float amount, float completedDurationSeconds, string localFocusDate, float goldenBonusIncrement)
    {
        SkillProgressResult result = new SkillProgressResult
        {
            skillId = NormalizeId(id)
        };

        SkillEntry skill = GetSkillById(id);
        if (skill == null || amount <= 0f)
        {
            return result;
        }

        float previousPercent = skill.percent;
        float newPercent = Mathf.Clamp(skill.percent + amount, 0f, 100f);

        skill.percent = newPercent;
        skill.totalFocusMinutes += Mathf.Max(0, Mathf.CeilToInt(completedDurationSeconds / 60f));
        skill.lastFocusDate = localFocusDate ?? string.Empty;

        bool becameGolden = !skill.isGolden && previousPercent < 100f && newPercent >= 100f;
        if (becameGolden)
        {
            skill.isGolden = true;
            skill.bonusExpMultiplier = Mathf.Max(skill.bonusExpMultiplier, goldenBonusIncrement);
        }

        result.success = true;
        result.deltaApplied = newPercent - previousPercent;
        result.previousPercent = previousPercent;
        result.newPercent = newPercent;
        result.becameGolden = becameGolden;
        result.isGolden = skill.isGolden;
        return result;
    }

    public float CalculateProgressFromFocusDuration(
        float durationSeconds,
        int petLevel,
        float petMoodPercent,
        float skillMinutesPerStep,
        float skillLevelMultiplierStep,
        float skillMoodBaseBonus,
        float skillMoodScale)
    {
        if (durationSeconds <= 0f || skillMinutesPerStep <= 0f)
        {
            return 0f;
        }

        float minutes = durationSeconds / 60f;
        float clampedMood = Mathf.Clamp(petMoodPercent, 0f, 100f);
        float levelMultiplier = GetPetLevelMultiplier(petLevel, skillLevelMultiplierStep);
        float baseGain = (minutes / skillMinutesPerStep) * levelMultiplier;
        float moodBonus = skillMoodBaseBonus + (clampedMood * skillMoodScale);

        if (moodBonus <= 0f)
        {
            return 0f;
        }

        return Mathf.Max(0f, baseGain * moodBonus);
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
            skill.icon = skill.icon ?? string.Empty;
            skill.percent = Mathf.Clamp(skill.percent, 0f, 100f);
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
