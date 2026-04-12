using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionPanelTests
{
    [Test]
    public void RebuildContent_ReusesMissionRowsAcrossRefreshes()
    {
        GameObject panelObject = new GameObject("MissionPanel", typeof(RectTransform));
        try
        {
            MissionPanelUI panel = panelObject.AddComponent<MissionPanelUI>();
            RectTransform content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(panelObject.transform, false);
            panel.emptyStateText = new GameObject("EmptyState", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();

            SetPrivateField(panel, "scrollContent", content);

            MethodInfo rebuildMethod = typeof(MissionPanelUI).GetMethod("RebuildContent", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(rebuildMethod);

            List<MissionEntryData> skillMissions = new List<MissionEntryData>
            {
                new MissionEntryData { missionId = "skill_a", title = "Math Sprint", targetProgress = 30, progressMinutes = 10f, requiredMinutes = 30f, rewardCoins = 20, isSelected = true, hasSelectionState = true },
                new MissionEntryData { missionId = "skill_b", title = "Art Drill", targetProgress = 15, progressMinutes = 0f, requiredMinutes = 15f, rewardCoins = 10 }
            };
            List<MissionEntryData> routines = new List<MissionEntryData>
            {
                new MissionEntryData { missionId = "routine_a", title = "Stretch", isRoutine = true, rewardCoins = 6 }
            };
            MissionBonusStatus bonus = new MissionBonusStatus
            {
                selectedSkillMissionCount = 2,
                completedSelectedSkillMissionCount = 1,
                isReady = false,
                isClaimed = false
            };

            rebuildMethod.Invoke(panel, new object[] { skillMissions, routines, bonus });
            List<MissionRowUI> firstPool = GetPrivateField<List<MissionRowUI>>(panel, "pooledMissionRows");
            int firstChildCount = content.childCount;
            MissionRowUI firstRow = firstPool[0];
            MissionRowUI secondRow = firstPool[1];
            MissionRowUI thirdRow = firstPool[2];

            rebuildMethod.Invoke(panel, new object[] { skillMissions, routines, bonus });
            List<MissionRowUI> secondPool = GetPrivateField<List<MissionRowUI>>(panel, "pooledMissionRows");

            Assert.AreEqual(firstChildCount, content.childCount);
            Assert.AreEqual(3, secondPool.Count);
            Assert.AreSame(firstRow, secondPool[0]);
            Assert.AreSame(secondRow, secondPool[1]);
            Assert.AreSame(thirdRow, secondPool[2]);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void RebuildSkillChoiceList_ReusesPopupButtons()
    {
        GameObject panelObject = new GameObject("MissionPanel", typeof(RectTransform));
        try
        {
            MissionPanelUI panel = panelObject.AddComponent<MissionPanelUI>();
            RectTransform root = new GameObject("PopupSkillList", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(panelObject.transform, false);

            MethodInfo rebuildMethod = typeof(MissionPanelUI).GetMethod("RebuildSkillChoiceList", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(rebuildMethod);

            List<Button> cache = GetPrivateField<List<Button>>(panel, "popupSkillButtons");
            List<SkillEntry> skills = new List<SkillEntry>
            {
                new SkillEntry { id = "skill_a", name = "Math" },
                new SkillEntry { id = "skill_b", name = "Art" }
            };

            System.Func<SkillEntry, string> labelFactory = skill => skill.name;
            System.Action<string> onSelect = _ => { };

            rebuildMethod.Invoke(panel, new object[] { root, cache, skills, "skill_a", labelFactory, onSelect });
            int firstChildCount = root.childCount;
            Button firstButton = cache[0];
            Button secondButton = cache[1];

            rebuildMethod.Invoke(panel, new object[] { root, cache, skills, "skill_b", labelFactory, onSelect });

            Assert.AreEqual(firstChildCount, root.childCount);
            Assert.AreEqual(2, cache.Count);
            Assert.AreSame(firstButton, cache[0]);
            Assert.AreSame(secondButton, cache[1]);
            Assert.True(cache[0].gameObject.activeSelf);
            Assert.True(cache[1].gameObject.activeSelf);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(panelObject);
        }
    }

    private static T GetPrivateField<T>(object target, string fieldName) where T : class
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return field.GetValue(target) as T;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(target, value);
    }
}
