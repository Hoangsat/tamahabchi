using System.Collections.Generic;
using UnityEngine;

public sealed class SkillDecaySystem
{
    private const float DecayIntervalSeconds = 3600f;

    private SkillsData skillsData;

    public void Init(SkillsData data)
    {
        skillsData = data ?? new SkillsData();
        skillsData.skills ??= new List<SkillEntry>();
    }

    public bool ApplyNeglectDecay(float neglectSeconds, ref float carrySeconds)
    {
        if (skillsData == null || neglectSeconds <= 0f)
        {
            return false;
        }

        float totalSeconds = Mathf.Max(0f, carrySeconds) + Mathf.Max(0f, neglectSeconds);
        int fullTicks = Mathf.FloorToInt(totalSeconds / DecayIntervalSeconds);
        carrySeconds = totalSeconds - (fullTicks * DecayIntervalSeconds);

        if (fullTicks <= 0 || skillsData.skills == null || skillsData.skills.Count == 0)
        {
            return false;
        }

        bool changed = false;
        for (int i = 0; i < skillsData.skills.Count; i++)
        {
            SkillEntry skill = skillsData.skills[i];
            if (skill == null)
            {
                continue;
            }

            int clampedTotal = SkillProgressionModel.ClampTotalSP(skill.totalSP);
            int clampedDebt = Mathf.Clamp(skill.decayDebtSP, 0, clampedTotal);
            int nextDebt = Mathf.Clamp(clampedDebt + fullTicks, 0, clampedTotal);
            if (nextDebt != clampedDebt)
            {
                skill.decayDebtSP = nextDebt;
                changed = true;
            }
        }

        return changed;
    }
}
