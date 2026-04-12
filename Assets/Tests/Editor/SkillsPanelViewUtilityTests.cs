using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;

public class SkillsPanelViewUtilityTests
{
    [Test]
    public void FindProgressionView_ReturnsMatchingEntry()
    {
        List<SkillProgressionViewData> views = new List<SkillProgressionViewData>
        {
            new SkillProgressionViewData { id = "skill_alpha", axisFill01 = 0.2f },
            new SkillProgressionViewData { id = "skill_beta", axisFill01 = 0.8f }
        };

        SkillProgressionViewData view = SkillsPanelViewUtility.FindProgressionView(views, "skill_beta");

        Assert.NotNull(view);
        Assert.AreEqual("skill_beta", view.id);
        Assert.AreEqual(0.8f, view.axisFill01);
    }

    [Test]
    public void GetChartPoint_FirstAxisStartsAtTop()
    {
        Vector2 point = SkillsPanelViewUtility.GetChartPoint(0, 4, 10f);

        Assert.AreEqual(0f, point.x, 0.001f);
        Assert.AreEqual(10f, point.y, 0.001f);
    }

    [Test]
    public void EnsureInstance_ReusesSpawnedComponent()
    {
        GameObject root = new GameObject("Root", typeof(RectTransform));
        GameObject templateObject = new GameObject("LabelTemplate", typeof(RectTransform), typeof(TextMeshProUGUI));
        try
        {
            RectTransform parent = root.GetComponent<RectTransform>();
            TextMeshProUGUI template = templateObject.GetComponent<TextMeshProUGUI>();
            List<TextMeshProUGUI> spawned = new List<TextMeshProUGUI>();

            TextMeshProUGUI first = SkillsPanelViewUtility.EnsureInstance(spawned, 0, template, parent);
            TextMeshProUGUI second = SkillsPanelViewUtility.EnsureInstance(spawned, 0, template, parent);

            Assert.AreSame(first, second);
            Assert.AreEqual(1, spawned.Count);
        }
        finally
        {
            Object.DestroyImmediate(templateObject);
            Object.DestroyImmediate(root);
        }
    }
}
