using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class SkillsPanelChartUtilityTests
{
    [Test]
    public void UpdateChartPresentation_WithTwoSkills_ShowsUnlockGuidanceAndHidesLabels()
    {
        GameObject root = new GameObject("ChartRoot", typeof(RectTransform));
        GameObject titleObject = new GameObject("ChartTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        GameObject emptyObject = new GameObject("ChartEmpty", typeof(RectTransform), typeof(TextMeshProUGUI));
        GameObject labelsObject = new GameObject("RadarLabels", typeof(RectTransform));

        try
        {
            TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI empty = emptyObject.GetComponent<TextMeshProUGUI>();
            RectTransform labelsRoot = labelsObject.GetComponent<RectTransform>();
            labelsRoot.SetParent(root.transform, false);

            List<SkillEntry> skills = new List<SkillEntry>
            {
                new SkillEntry { id = "skill_one", name = "One" },
                new SkillEntry { id = "skill_two", name = "Two" }
            };

            SkillsPanelChartUtility.UpdateChartPresentation(title, empty, labelsRoot, skills, 3);

            StringAssert.Contains("2/3", title.text);
            StringAssert.Contains("Add 1 more skill", empty.text);
            Assert.False(labelsRoot.gameObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(titleObject);
            Object.DestroyImmediate(emptyObject);
            Object.DestroyImmediate(labelsObject);
        }
    }
}
