using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class SkillsPanelViewUtility
{
    public static SkillProgressionViewData FindProgressionView(List<SkillProgressionViewData> views, string skillId)
    {
        if (views == null || string.IsNullOrEmpty(skillId))
        {
            return null;
        }

        for (int i = 0; i < views.Count; i++)
        {
            SkillProgressionViewData view = views[i];
            if (view != null && view.id == skillId)
            {
                return view;
            }
        }

        return null;
    }

    public static T EnsureInstance<T>(List<T> spawnedInstances, int index, T template, Transform parent) where T : Component
    {
        while (spawnedInstances.Count <= index)
        {
            T instance = Object.Instantiate(template, parent);
            instance.gameObject.SetActive(false);
            spawnedInstances.Add(instance);
        }

        return spawnedInstances[index];
    }

    public static void HideUnused<T>(List<T> spawnedInstances, int usedCount) where T : Component
    {
        for (int i = usedCount; i < spawnedInstances.Count; i++)
        {
            if (spawnedInstances[i] != null)
            {
                spawnedInstances[i].gameObject.SetActive(false);
            }
        }
    }

    public static Vector2 GetChartPoint(int index, int count, float radius)
    {
        float angle = Mathf.PI * 0.5f - (Mathf.PI * 2f * index / count);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }
}
