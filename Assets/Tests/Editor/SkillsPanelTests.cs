using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillsPanelTests
{
    [Test]
    public void SkillsSystem_PreventsDuplicateSkillNamesIgnoringCase()
    {
        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(new SkillsData());

        SkillEntry first = skillsSystem.AddSkill("Coding", "DEV");
        SkillEntry duplicate = skillsSystem.AddSkill(" coding ", "ART");

        Assert.NotNull(first);
        Assert.Null(duplicate);
        Assert.True(skillsSystem.HasSkillName("CODING"));
    }

    [Test]
    public void BuildChartSkills_PreservesSourceOrderForStableAxes()
    {
        GameObject panelObject = new GameObject("SkillsPanel");
        try
        {
            SkillsPanelUI panel = panelObject.AddComponent<SkillsPanelUI>();
            MethodInfo method = typeof(SkillsPanelUI).GetMethod("BuildChartSkills", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            List<SkillEntry> source = new List<SkillEntry>
            {
                new SkillEntry { id = "skill_alpha", name = "Alpha", percent = 5f },
                new SkillEntry { id = "skill_beta", name = "Beta", percent = 90f },
                new SkillEntry { id = "skill_gamma", name = "Gamma", percent = 25f }
            };

            IEnumerable result = method.Invoke(panel, new object[] { source }) as IEnumerable;
            Assert.NotNull(result);

            List<string> actualIds = new List<string>();
            foreach (object item in result)
            {
                FieldInfo skillField = item.GetType().GetField("Skill", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.NotNull(skillField);
                SkillEntry skill = skillField.GetValue(item) as SkillEntry;
                actualIds.Add(skill != null ? skill.id : string.Empty);
            }

            CollectionAssert.AreEqual(new[] { "skill_alpha", "skill_beta", "skill_gamma" }, actualIds);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void HandleSkillProgressAdded_DoesNotShowPopupWhenPanelIsHidden()
    {
        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(new SkillsData
        {
            skills = new List<SkillEntry>
            {
                new SkillEntry { id = "skill_hidden", name = "Hidden", icon = "DEV", percent = 10f }
            }
        });

        GameObject managerObject = new GameObject("GameManager");
        GameObject panelObject = new GameObject("SkillsPanel", typeof(RectTransform));
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();
            typeof(GameManager)
                .GetField("skillsSystem", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, skillsSystem);

            SkillsPanelUI panel = panelObject.AddComponent<SkillsPanelUI>();
            panel.panelRoot = new GameObject("PanelRoot");
            panel.panelRoot.SetActive(false);
            panel.skillGainPopupRoot = new GameObject("SkillGainPopup", typeof(RectTransform), typeof(CanvasGroup));
            panel.skillGainPopupCanvasGroup = panel.skillGainPopupRoot.GetComponent<CanvasGroup>();
            panel.skillGainPopupTransform = panel.skillGainPopupRoot.GetComponent<RectTransform>();
            panel.SetGameManager(manager);

            MethodInfo method = typeof(SkillsPanelUI).GetMethod("HandleSkillProgressAdded", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(panel, new object[] { "skill_hidden", 5f, 15f });

            Assert.False(panel.skillGainPopupRoot.activeSelf);
            Assert.AreEqual(0f, panel.skillGainPopupCanvasGroup.alpha, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void ShowPanel_WithLessThanThreeSkills_ShowsUnlockGuidance()
    {
        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(new SkillsData
        {
            skills = new List<SkillEntry>
            {
                new SkillEntry { id = "skill_one", name = "One", icon = "DEV", percent = 10f },
                new SkillEntry { id = "skill_two", name = "Two", icon = "ART", percent = 20f }
            }
        });

        GameObject managerObject = new GameObject("GameManager");
        GameObject panelObject = new GameObject("SkillsPanel", typeof(RectTransform));
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();
            typeof(GameManager)
                .GetField("skillsSystem", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, skillsSystem);

            SkillsPanelUI panel = panelObject.AddComponent<SkillsPanelUI>();
            panel.panelRoot = new GameObject("PanelRoot");
            panel.chartTitleText = new GameObject("ChartTitle", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.chartEmptyStateText = new GameObject("ChartEmpty", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.emptyStateText = new GameObject("ListEmpty", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.panelSelectedSkillText = new GameObject("SelectedSkill", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.focusSkillStatusText = new GameObject("FocusSkill", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.addSkillButton = new GameObject("AddSkillButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            panel.addSkillButtonText = new GameObject("AddSkillButtonText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.SetGameManager(manager);

            panel.ShowPanel();

            StringAssert.Contains("2/3", panel.chartTitleText.text);
            StringAssert.Contains("Add 1 more skill", panel.chartEmptyStateText.text);
            Assert.True(panel.chartEmptyStateText.gameObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void RefreshUI_ReusesExistingRowsInsteadOfGrowingChildren()
    {
        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(new SkillsData
        {
            skills = new List<SkillEntry>
            {
                new SkillEntry { id = "skill_one", name = "One", icon = "DEV", percent = 10f },
                new SkillEntry { id = "skill_two", name = "Two", icon = "ART", percent = 20f }
            }
        });

        GameObject managerObject = new GameObject("GameManager");
        GameObject panelObject = new GameObject("SkillsPanel", typeof(RectTransform));
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();
            typeof(GameManager)
                .GetField("skillsSystem", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, skillsSystem);

            SkillsPanelUI panel = panelObject.AddComponent<SkillsPanelUI>();
            panel.panelRoot = new GameObject("PanelRoot");
            panel.chartTitleText = new GameObject("ChartTitle", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.chartEmptyStateText = new GameObject("ChartEmpty", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.emptyStateText = new GameObject("ListEmpty", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.panelSelectedSkillText = new GameObject("SelectedSkill", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.focusSkillStatusText = new GameObject("FocusSkill", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.addSkillButton = new GameObject("AddSkillButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            panel.addSkillButtonText = new GameObject("AddSkillButtonText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.skillsListContainer = new GameObject("SkillsList", typeof(RectTransform)).GetComponent<RectTransform>();
            panel.skillsListContainer.SetParent(panelObject.transform, false);

            GameObject templateObject = new GameObject("SkillRowTemplate", typeof(RectTransform), typeof(Image), typeof(SkillRowUI));
            templateObject.transform.SetParent(panel.skillsListContainer, false);
            SkillRowUI template = templateObject.GetComponent<SkillRowUI>();
            template.backgroundImage = template.GetComponent<Image>();
            panel.skillRowTemplate = template;
            panel.SetGameManager(manager);

            panel.ShowPanel();
            int firstChildCount = panel.skillsListContainer.childCount;

            MethodInfo refreshMethod = typeof(SkillsPanelUI).GetMethod("RefreshUI", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(refreshMethod);
            refreshMethod.Invoke(panel, null);

            int secondChildCount = panel.skillsListContainer.childCount;

            Assert.AreEqual(firstChildCount, secondChildCount);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void ShowPanel_KeepsCloseButtonHiddenWhenShellNavigationIsPrimary()
    {
        GameObject panelObject = new GameObject("SkillsPanel", typeof(RectTransform));
        try
        {
            SkillsPanelUI panel = panelObject.AddComponent<SkillsPanelUI>();
            panel.panelRoot = new GameObject("PanelRoot");
            panel.closeButton = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();

            MethodInfo awakeMethod = typeof(SkillsPanelUI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(awakeMethod);
            awakeMethod.Invoke(panel, null);

            panel.ShowPanel();
            Assert.False(panel.closeButton.gameObject.activeSelf);

            panel.HidePanel();
            Assert.False(panel.closeButton.gameObject.activeSelf);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void HidePanel_ClearsTransientPopupAndPendingFeedback()
    {
        GameObject panelObject = new GameObject("SkillsPanel", typeof(RectTransform));
        try
        {
            SkillsPanelUI panel = panelObject.AddComponent<SkillsPanelUI>();
            panel.panelRoot = new GameObject("PanelRoot");
            panel.panelRoot.SetActive(true);
            panel.skillGainPopupRoot = new GameObject("SkillGainPopup", typeof(RectTransform), typeof(CanvasGroup));
            panel.skillGainPopupCanvasGroup = panel.skillGainPopupRoot.GetComponent<CanvasGroup>();
            panel.skillGainPopupTransform = panel.skillGainPopupRoot.GetComponent<RectTransform>();
            panel.skillGainPopupRoot.SetActive(true);
            panel.skillGainPopupCanvasGroup.alpha = 1f;
            panel.closeButton = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();

            typeof(SkillsPanelUI).GetField("pendingFeedbackSkillId", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(panel, "skill_test");
            typeof(SkillsPanelUI).GetField("pendingFeedbackDelta", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(panel, 5f);
            typeof(SkillsPanelUI).GetField("pendingFeedbackNewPercent", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(panel, 25f);

            MethodInfo awakeMethod = typeof(SkillsPanelUI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(awakeMethod);
            awakeMethod.Invoke(panel, null);

            panel.ShowPanel();
            panel.HidePanel();

            Assert.False(panel.skillGainPopupRoot.activeSelf);
            Assert.AreEqual(0f, panel.skillGainPopupCanvasGroup.alpha, 0.001f);
            Assert.AreEqual(string.Empty, typeof(SkillsPanelUI).GetField("pendingFeedbackSkillId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel));
            Assert.AreEqual(0f, (float)typeof(SkillsPanelUI).GetField("pendingFeedbackDelta", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel), 0.001f);
            Assert.AreEqual(0f, (float)typeof(SkillsPanelUI).GetField("pendingFeedbackNewPercent", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel), 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void RefreshAddButtonState_DisablesDuplicateSkillName()
    {
        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(new SkillsData
        {
            skills = new List<SkillEntry>
            {
                new SkillEntry { id = "skill_existing", name = "Coding", icon = "DEV", percent = 10f }
            }
        });

        GameObject managerObject = new GameObject("GameManager");
        GameObject panelObject = new GameObject("SkillsPanel", typeof(RectTransform));
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();
            typeof(GameManager)
                .GetField("skillsSystem", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, skillsSystem);

            SkillsPanelUI panel = panelObject.AddComponent<SkillsPanelUI>();
            panel.addSkillButton = new GameObject("AddSkillButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            panel.addSkillButtonText = new GameObject("AddSkillButtonText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.skillNameInput = new GameObject("SkillNameInput", typeof(RectTransform), typeof(TMP_InputField)).GetComponent<TMP_InputField>();
            panel.skillNameInput.text = " coding ";
            panel.SetGameManager(manager);

            MethodInfo refreshMethod = typeof(SkillsPanelUI).GetMethod("RefreshAddButtonState", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(refreshMethod);
            refreshMethod.Invoke(panel, null);

            Assert.False(panel.addSkillButton.interactable);
            Assert.AreEqual("Exists", panel.addSkillButtonText.text);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void RefreshHeroBlock_UsesSelectedSkillDataForHeroCard()
    {
        GameObject panelObject = new GameObject("SkillsPanel", typeof(RectTransform));
        try
        {
            SkillsPanelUI panel = panelObject.AddComponent<SkillsPanelUI>();
            panel.heroSkillNameText = new GameObject("HeroSkillName", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.heroSkillMetaText = new GameObject("HeroSkillMeta", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.heroProgressText = new GameObject("HeroProgressText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.heroHintText = new GameObject("HeroHintText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.heroActionButton = new GameObject("HeroActionButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            panel.heroActionButtonText = new GameObject("HeroActionLabel", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.heroProgressFillImage = new GameObject("HeroFill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();

            MethodInfo method = typeof(SkillsPanelUI).GetMethod("RefreshHeroBlock", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            List<SkillEntry> skills = new List<SkillEntry>
            {
                new SkillEntry { id = "skill_alpha", name = "Alpha", icon = "DEV", percent = 20f, totalFocusMinutes = 25 },
                new SkillEntry { id = "skill_beta", name = "Beta", icon = "ART", percent = 82f, totalFocusMinutes = 90 }
            };

            method.Invoke(panel, new object[] { skills, "skill_beta" });

            Assert.AreEqual("Beta", panel.heroSkillNameText.text);
            StringAssert.Contains("Selected", panel.heroSkillMetaText.text);
            Assert.AreEqual("82% tracked", panel.heroProgressText.text);
            Assert.AreEqual("Start Focus", panel.heroActionButtonText.text);
            Assert.Greater(panel.heroProgressFillImage.fillAmount, 0.8f);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void RefreshHeroBlock_UsesBestSkillWhenNoFocusSelectionExists()
    {
        GameObject panelObject = new GameObject("SkillsPanel", typeof(RectTransform));
        try
        {
            SkillsPanelUI panel = panelObject.AddComponent<SkillsPanelUI>();
            panel.heroSkillNameText = new GameObject("HeroSkillName", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.heroSkillMetaText = new GameObject("HeroSkillMeta", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.heroProgressText = new GameObject("HeroProgressText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.heroHintText = new GameObject("HeroHintText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.heroActionButton = new GameObject("HeroActionButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            panel.heroActionButtonText = new GameObject("HeroActionLabel", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            panel.heroProgressFillImage = new GameObject("HeroFill", typeof(RectTransform), typeof(Image)).GetComponent<Image>();

            MethodInfo method = typeof(SkillsPanelUI).GetMethod("RefreshHeroBlock", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            List<SkillEntry> skills = new List<SkillEntry>
            {
                new SkillEntry { id = "skill_alpha", name = "Alpha", icon = "DEV", percent = 35f, totalFocusMinutes = 10 },
                new SkillEntry { id = "skill_beta", name = "Beta", icon = "ART", percent = 60f, totalFocusMinutes = 5 }
            };

            method.Invoke(panel, new object[] { skills, string.Empty });

            Assert.AreEqual("Beta", panel.heroSkillNameText.text);
            StringAssert.Contains("Recommended", panel.heroSkillMetaText.text);
            Assert.AreEqual("Focus This", panel.heroActionButtonText.text);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
        }
    }

    [Test]
    public void SkillRowBind_DisablesWordWrappingForReadableSingleLineLabels()
    {
        GameObject rowObject = new GameObject("SkillRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(SkillRowUI));
        try
        {
            SkillRowUI row = rowObject.GetComponent<SkillRowUI>();
            HorizontalLayoutGroup layoutGroup = rowObject.GetComponent<HorizontalLayoutGroup>();
            row.backgroundImage = rowObject.GetComponent<Image>();
            row.iconText = new GameObject("IconText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            row.nameText = new GameObject("NameText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            row.percentText = new GameObject("PercentText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            row.selectButton = new GameObject("SelectButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            row.removeButton = new GameObject("RemoveButton", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            row.selectButtonText = new GameObject("SelectLabel", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            row.removeButtonText = new GameObject("RemoveLabel", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();

            row.iconText.transform.SetParent(rowObject.transform, false);
            row.nameText.transform.SetParent(rowObject.transform, false);
            row.percentText.transform.SetParent(rowObject.transform, false);
            row.selectButton.transform.SetParent(rowObject.transform, false);
            row.removeButton.transform.SetParent(rowObject.transform, false);

            MethodInfo awakeMethod = typeof(SkillRowUI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(awakeMethod);
            awakeMethod.Invoke(row, null);

            row.Bind(
                new SkillEntry { id = "skill_alpha", name = "VeryLongSkillName", icon = "DEV", percent = 25f },
                Color.cyan,
                false,
                _ => { },
                _ => { });

            Assert.AreEqual(TextWrappingModes.NoWrap, row.iconText.textWrappingMode);
            Assert.AreEqual(TextWrappingModes.NoWrap, row.nameText.textWrappingMode);
            Assert.AreEqual(TextWrappingModes.NoWrap, row.percentText.textWrappingMode);
            Assert.True(layoutGroup.childControlWidth);
            Assert.True(layoutGroup.childControlHeight);
        }
        finally
        {
            Object.DestroyImmediate(rowObject);
        }
    }
}
