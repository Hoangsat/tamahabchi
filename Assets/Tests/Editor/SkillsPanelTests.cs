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
    public void HandleSkillProgressAdded_DoesNotShowPopupWhenPanelIsHidden()
    {
        SkillsSystem skillsSystem = new SkillsSystem();
        skillsSystem.Init(new SkillsData
        {
            skills = new List<SkillEntry>
            {
                new SkillEntry { id = "skill_hidden", name = "Hidden", icon = "DEV", totalSP = 100 }
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
            method.Invoke(panel, new object[]
            {
                new SkillProgressResult
                {
                    skillId = "skill_hidden",
                    deltaSP = 15,
                    previousLevel = 0,
                    newLevel = 1,
                    previousAxisPercent = 9f,
                    newAxisPercent = 10.5f
                }
            });

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
                new SkillEntry { id = "skill_one", name = "One", icon = "DEV", totalSP = 100 },
                new SkillEntry { id = "skill_two", name = "Two", icon = "ART", totalSP = 200 }
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
                new SkillEntry { id = "skill_one", name = "One", icon = "DEV", totalSP = 100 },
                new SkillEntry { id = "skill_two", name = "Two", icon = "ART", totalSP = 200 }
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
            typeof(SkillsPanelUI).GetField("pendingFeedbackResult", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(panel, new SkillProgressResult { skillId = "skill_test", deltaSP = 15, newLevel = 2 });

            MethodInfo awakeMethod = typeof(SkillsPanelUI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(awakeMethod);
            awakeMethod.Invoke(panel, null);

            panel.ShowPanel();
            panel.HidePanel();

            Assert.False(panel.skillGainPopupRoot.activeSelf);
            Assert.AreEqual(0f, panel.skillGainPopupCanvasGroup.alpha, 0.001f);
            Assert.AreEqual(string.Empty, typeof(SkillsPanelUI).GetField("pendingFeedbackSkillId", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel));
            Assert.IsNull(typeof(SkillsPanelUI).GetField("pendingFeedbackResult", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(panel));
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
                new SkillEntry { id = "skill_existing", name = "Coding", icon = "DEV", totalSP = 100 }
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

            SkillProgressionViewData heroView = new SkillProgressionViewData
            {
                id = "skill_beta",
                level = SkillProgressionModel.GetLevel(900),
                progressInLevel01 = SkillProgressionModel.GetProgressInLevel01(900),
                progressToNextLevelPercent = SkillProgressionModel.GetProgressInLevel01(900) * 100f,
                axisPercent = SkillProgressionModel.GetAxisPercent(900),
                axisFill01 = SkillProgressionModel.GetAxisPercent(900) / 100f,
                totalFocusMinutes = 90
            };
            SkillsHeroState heroState = new SkillsHeroState(
                new SkillEntry { id = "skill_beta", name = "Beta", icon = "ART", totalSP = 900, totalFocusMinutes = 90 },
                heroView,
                true,
                "Current Focus",
                "Current Focus: Beta - Lv." + heroView.level,
                "Beta",
                "Selected Focus | Axis 90% | 90m logged",
                $"LEVEL {heroView.level}\n{heroView.progressToNextLevelPercent:0.#}% to Lv.{Mathf.Min(heroView.level + 1, SkillProgressionModel.MaxLevel)}",
                "Progress snapshot",
                "Start Focus");

            method.Invoke(panel, new object[] { heroState });

            Assert.AreEqual("Beta", panel.heroSkillNameText.text);
            StringAssert.Contains("Selected", panel.heroSkillMetaText.text);
            StringAssert.Contains("Lv.", panel.heroProgressText.text);
            Assert.AreEqual("Start Focus", panel.heroActionButtonText.text);
            Assert.Greater(panel.heroProgressFillImage.fillAmount, 0f);
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

            SkillProgressionViewData heroView = new SkillProgressionViewData
            {
                id = "skill_beta",
                level = SkillProgressionModel.GetLevel(600),
                progressToNextLevelPercent = SkillProgressionModel.GetProgressInLevel01(600) * 100f,
                axisPercent = SkillProgressionModel.GetAxisPercent(600),
                axisFill01 = SkillProgressionModel.GetAxisPercent(600) / 100f,
                totalFocusMinutes = 5
            };
            SkillsHeroState heroState = new SkillsHeroState(
                new SkillEntry { id = "skill_beta", name = "Beta", icon = "ART", totalSP = 600, totalFocusMinutes = 5 },
                heroView,
                false,
                "Top Skill",
                "Current Focus: Beta - Lv." + heroView.level,
                "Beta",
                "Recommended Focus | Axis 60% | 5m logged",
                $"LEVEL {heroView.level}\n{heroView.progressToNextLevelPercent:0.#}% to Lv.{Mathf.Min(heroView.level + 1, SkillProgressionModel.MaxLevel)}",
                "Strong candidate for your next focus run.",
                "Focus This");

            method.Invoke(panel, new object[] { heroState });

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
                new SkillEntry { id = "skill_alpha", name = "VeryLongSkillName", icon = "DEV", totalSP = 250 },
                new SkillProgressionViewData { id = "skill_alpha", level = 3, progressToNextLevelPercent = 25f, totalSP = 250 },
                Color.cyan,
                false,
                "Логика и мышление",
                _ => { },
                _ => { },
                _ => { });

            Assert.AreEqual(TextWrappingModes.NoWrap, row.iconText.textWrappingMode);
            Assert.AreEqual(TextWrappingModes.NoWrap, row.nameText.textWrappingMode);
            Assert.AreEqual(TextWrappingModes.NoWrap, row.percentText.textWrappingMode);
            Assert.NotNull(row.typeButton);
            Assert.NotNull(row.typeButtonText);
            Assert.AreEqual("Логика и мышление", row.typeButtonText.text);
            Assert.True(layoutGroup.childControlWidth);
            Assert.True(layoutGroup.childControlHeight);
        }
        finally
        {
            Object.DestroyImmediate(rowObject);
        }
    }

    [Test]
    public void ShowPanel_UsesCompactLayoutOnShortCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        GameObject panelObject = new GameObject("SkillsPanelUIRoot", typeof(RectTransform));
        try
        {
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(720f, 720f);
            panelObject.transform.SetParent(canvasObject.transform, false);

            SkillsPanelUI panel = panelObject.AddComponent<SkillsPanelUI>();
            panel.panelRoot = CreateSkillsPanelHierarchy(panelObject.transform as RectTransform, panel);

            MethodInfo awakeMethod = typeof(SkillsPanelUI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(awakeMethod);
            awakeMethod.Invoke(panel, null);

            panel.ShowPanel();
            Canvas.ForceUpdateCanvases();

            LayoutElement heroLayout = panel.panelRoot.transform.Find("SkillsCard/HeroCard").GetComponent<LayoutElement>();
            Assert.AreEqual(224f * 0.86f, heroLayout.preferredHeight, 0.01f);

            LayoutElement chartLayout = panel.panelRoot.transform.Find("SkillsCard/ChartContainer").GetComponent<LayoutElement>();
            Assert.AreEqual(408f * 0.84f, chartLayout.preferredHeight, 0.01f);

            LayoutElement inputLayout = panel.panelRoot.transform.Find("SkillsCard/SkillNameInput").GetComponent<LayoutElement>();
            Assert.AreEqual(60f * 0.90f, inputLayout.preferredHeight, 0.01f);

            HorizontalLayoutGroup iconRowLayout = panel.panelRoot.transform.Find("SkillsCard/IconRow").GetComponent<HorizontalLayoutGroup>();
            Assert.AreEqual(10f * 0.80f, iconRowLayout.spacing, 0.01f);

            Assert.AreEqual(32f * 0.88f, panel.heroSkillNameText.fontSize, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void SkillRow_Bind_UsesCompactSizingOnShortCanvas()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject rowObject = new GameObject("SkillRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        try
        {
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(720f, 720f);

            rowObject.transform.SetParent(canvasObject.transform, false);
            LayoutElement rowLayout = rowObject.GetComponent<LayoutElement>();
            rowLayout.preferredHeight = 60f;

            HorizontalLayoutGroup rowLayoutGroup = rowObject.GetComponent<HorizontalLayoutGroup>();
            rowLayoutGroup.spacing = 10f;

            SkillRowUI row = rowObject.AddComponent<SkillRowUI>();
            row.backgroundImage = rowObject.GetComponent<Image>();
            row.colorMarkerImage = new GameObject("ColorMarker", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            row.colorMarkerImage.transform.SetParent(rowObject.transform, false);
            row.iconText = CreateText("IconText", rowObject.transform, 24f);
            row.nameText = CreateText("NameText", rowObject.transform, 18f);
            row.percentText = CreateText("PercentText", rowObject.transform, 16f);
            row.selectButton = CreateButton("SelectButton", rowObject.transform, out TextMeshProUGUI selectLabel, 18f);
            row.selectButtonText = selectLabel;
            row.removeButton = CreateButton("RemoveButton", rowObject.transform, out TextMeshProUGUI removeLabel, 18f);
            row.removeButtonText = removeLabel;

            MethodInfo awakeMethod = typeof(SkillRowUI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(awakeMethod);
            awakeMethod.Invoke(row, null);

            row.Bind(
                new SkillEntry { id = "skill_one", name = "Coding", icon = "DEV", totalSP = 100 },
                new SkillProgressionViewData
                {
                    id = "skill_one",
                    level = 1,
                    progressToNextLevelPercent = 50f,
                    progressInLevel = 10,
                    requiredSPForNextLevel = 20,
                    totalSP = 100
                },
                Color.cyan,
                false,
                "Логика и мышление",
                _ => { },
                _ => { },
                _ => { });

            Assert.AreEqual(60f * 0.88f, rowLayout.preferredHeight, 0.01f);
            Assert.AreEqual(10f * 0.85f, rowLayoutGroup.spacing, 0.01f);
            Assert.AreEqual(14f * 0.86f, row.iconText.fontSize, 0.01f);
            Assert.AreEqual(18f * 0.86f, row.selectButtonText.fontSize, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(rowObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void ShowPanel_CreatesArchetypeCardsAndUsesDefaultSelection()
    {
        GameObject managerObject = new GameObject("GameManager");
        GameObject panelObject = new GameObject("SkillsPanelUIRoot", typeof(RectTransform));
        try
        {
            SkillsData data = new SkillsData();
            GameManager manager = managerObject.AddComponent<GameManager>();
            manager.skillsData = data;

            SkillsSystem skillsSystem = new SkillsSystem();
            skillsSystem.Init(data);
            typeof(GameManager)
                .GetField("skillsSystem", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, skillsSystem);

            SkillsPanelUI panel = panelObject.AddComponent<SkillsPanelUI>();
            panel.panelRoot = CreateSkillsPanelHierarchy(panelObject.transform as RectTransform, panel);
            panel.SetGameManager(manager);

            MethodInfo awakeMethod = typeof(SkillsPanelUI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(awakeMethod);
            awakeMethod.Invoke(panel, null);

            panel.skillNameInput.text = "Python";
            panel.ShowPanel();

            Transform picker = panel.panelRoot.transform.Find("SkillsCard/ArchetypePicker");
            Assert.NotNull(picker);
            Assert.AreEqual(SkillArchetypeCatalog.GetPlayerSelectableDefinitions().Count, picker.childCount);
            Assert.True(panel.addSkillButton.interactable);

            string selectedArchetypeId = (string)typeof(SkillsPanelUI)
                .GetField("selectedCreateArchetypeId", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(panel);
            Assert.AreEqual(SkillArchetypeCatalog.Logic, selectedArchetypeId);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(managerObject);
        }
    }

    [Test]
    public void ConfirmArchetypeEdit_UpdatesSkillAndHeroIcon()
    {
        GameObject managerObject = new GameObject("GameManager");
        GameObject panelObject = new GameObject("SkillsPanelUIRoot", typeof(RectTransform));
        try
        {
            SkillsData data = new SkillsData
            {
                skills =
                {
                    new SkillEntry
                    {
                        id = "skill_code",
                        name = "Coding",
                        icon = "MTH",
                        archetypeId = SkillArchetypeCatalog.Logic,
                        totalSP = 300
                    }
                }
            };

            GameManager manager = managerObject.AddComponent<GameManager>();
            manager.skillsData = data;

            SkillsSystem skillsSystem = new SkillsSystem();
            skillsSystem.Init(data);
            typeof(GameManager)
                .GetField("skillsSystem", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(manager, skillsSystem);

            SkillsPanelUI panel = panelObject.AddComponent<SkillsPanelUI>();
            panel.panelRoot = CreateSkillsPanelHierarchy(panelObject.transform as RectTransform, panel);
            panel.SetGameManager(manager);

            MethodInfo awakeMethod = typeof(SkillsPanelUI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awakeMethod.Invoke(panel, null);
            panel.ShowPanel();

            MethodInfo openPopupMethod = typeof(SkillsPanelUI).GetMethod("OnChangeSkillType", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo selectPopupMethod = typeof(SkillsPanelUI).GetMethod("OnSelectEditArchetype", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo confirmMethod = typeof(SkillsPanelUI).GetMethod("ConfirmArchetypeEdit", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.NotNull(openPopupMethod);
            Assert.NotNull(selectPopupMethod);
            Assert.NotNull(confirmMethod);

            openPopupMethod.Invoke(panel, new object[] { "skill_code" });

            string pendingArchetypeId = (string)typeof(SkillsPanelUI)
                .GetField("pendingEditArchetypeId", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(panel);
            Assert.AreEqual(SkillArchetypeCatalog.Logic, pendingArchetypeId);

            selectPopupMethod.Invoke(panel, new object[] { SkillArchetypeCatalog.Music });
            confirmMethod.Invoke(panel, null);

            SkillEntry updatedSkill = manager.GetSkillById("skill_code");
            Assert.NotNull(updatedSkill);
            Assert.AreEqual(SkillArchetypeCatalog.Music, updatedSkill.archetypeId);
            Assert.AreEqual("MSC", updatedSkill.icon);
            Assert.AreEqual("MSC", panel.heroSkillIconText.text);
        }
        finally
        {
            Object.DestroyImmediate(panelObject);
            Object.DestroyImmediate(managerObject);
        }
    }

    private static GameObject CreateSkillsPanelHierarchy(RectTransform parent, SkillsPanelUI panel)
    {
        GameObject panelRoot = new GameObject("SkillsPanel", typeof(RectTransform), typeof(Image));
        panelRoot.transform.SetParent(parent, false);

        GameObject skillsCard = new GameObject("SkillsCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        skillsCard.transform.SetParent(panelRoot.transform, false);
        VerticalLayoutGroup skillsCardLayout = skillsCard.GetComponent<VerticalLayoutGroup>();
        skillsCardLayout.spacing = 14f;
        skillsCardLayout.padding = new RectOffset(22, 22, 20, 20);

        GameObject headerRow = new GameObject("HeaderRow", typeof(RectTransform));
        headerRow.transform.SetParent(skillsCard.transform, false);
        new GameObject("TitleText", typeof(RectTransform)).transform.SetParent(headerRow.transform, false);

        panel.closeButton = CreateButton("CloseButton", headerRow.transform, out _, 18f);
        LayoutElement closeLayout = panel.closeButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 76f;
        closeLayout.preferredHeight = 36f;

        GameObject heroCard = new GameObject("HeroCard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        heroCard.transform.SetParent(skillsCard.transform, false);
        heroCard.GetComponent<LayoutElement>().preferredHeight = 224f;
        VerticalLayoutGroup heroLayout = heroCard.GetComponent<VerticalLayoutGroup>();
        heroLayout.spacing = 12f;
        heroLayout.padding = new RectOffset(18, 18, 14, 16);
        panel.heroCardBackgroundImage = heroCard.GetComponent<Image>();

        GameObject heroIconBadge = new GameObject("HeroIconBadge", typeof(RectTransform), typeof(Image));
        heroIconBadge.transform.SetParent(heroCard.transform, false);
        panel.heroIconBadgeImage = heroIconBadge.GetComponent<Image>();
        panel.heroSkillIconText = CreateText("HeroSkillIconText", heroIconBadge.transform, 38f);
        panel.panelSelectedSkillText = CreateText("SelectedSkillText", heroCard.transform, 18f);
        panel.heroSkillNameText = CreateText("HeroSkillNameText", heroCard.transform, 32f);
        panel.heroSkillMetaText = CreateText("HeroSkillMetaText", heroCard.transform, 18f);

        GameObject heroProgressFill = new GameObject("HeroProgressFill", typeof(RectTransform), typeof(Image));
        heroProgressFill.transform.SetParent(heroCard.transform, false);
        panel.heroProgressFillImage = heroProgressFill.GetComponent<Image>();
        panel.heroProgressText = CreateText("HeroProgressText", heroCard.transform, 22f);
        panel.heroHintText = CreateText("HeroHintText", heroCard.transform, 16f);
        panel.heroActionButton = CreateButton("HeroActionButton", heroCard.transform, out TextMeshProUGUI heroActionLabel, 18f);
        LayoutElement heroActionLayout = panel.heroActionButton.gameObject.AddComponent<LayoutElement>();
        heroActionLayout.preferredWidth = 118f;
        heroActionLayout.preferredHeight = 42f;
        panel.heroActionButtonText = heroActionLabel;

        GameObject chartContainer = new GameObject("ChartContainer", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        chartContainer.transform.SetParent(skillsCard.transform, false);
        chartContainer.GetComponent<LayoutElement>().preferredHeight = 408f;
        VerticalLayoutGroup chartLayout = chartContainer.GetComponent<VerticalLayoutGroup>();
        chartLayout.spacing = 16f;
        chartLayout.padding = new RectOffset(16, 16, 16, 16);
        panel.chartTitleText = CreateText("ChartTitleText", chartContainer.transform, 20f);
        panel.chartEmptyStateText = CreateText("ChartEmptyStateText", chartContainer.transform, 18f);

        GameObject inputObject = new GameObject("SkillNameInput", typeof(RectTransform), typeof(LayoutElement), typeof(TMP_InputField));
        inputObject.transform.SetParent(skillsCard.transform, false);
        inputObject.GetComponent<LayoutElement>().preferredHeight = 60f;
        panel.skillNameInput = inputObject.GetComponent<TMP_InputField>();

        GameObject iconRow = new GameObject("IconRow", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
        iconRow.transform.SetParent(skillsCard.transform, false);
        iconRow.GetComponent<LayoutElement>().preferredHeight = 58f;
        iconRow.GetComponent<HorizontalLayoutGroup>().spacing = 10f;
        panel.iconPreviewText = CreateText("IconPreviewText", iconRow.transform, 26f);
        panel.addSkillButton = CreateButton("AddSkillButton", iconRow.transform, out TextMeshProUGUI addSkillLabel, 18f);
        LayoutElement addSkillLayout = panel.addSkillButton.gameObject.AddComponent<LayoutElement>();
        addSkillLayout.preferredWidth = 118f;
        addSkillLayout.preferredHeight = 36f;
        panel.addSkillButtonText = addSkillLabel;

        panel.panelStatusText = CreateText("PanelStatusText", skillsCard.transform, 18f);
        panel.emptyStateText = CreateText("EmptyStateText", skillsCard.transform, 18f);
        panel.focusSkillStatusText = CreateText("FocusSkillText", skillsCard.transform, 18f);

        GameObject skillsList = new GameObject("SkillsList", typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup), typeof(ScrollRect));
        skillsList.transform.SetParent(skillsCard.transform, false);
        LayoutElement skillsListLayout = skillsList.GetComponent<LayoutElement>();
        skillsListLayout.flexibleHeight = 1f;
        VerticalLayoutGroup listLayoutGroup = skillsList.GetComponent<VerticalLayoutGroup>();
        listLayoutGroup.padding = new RectOffset(12, 12, 12, 12);
        listLayoutGroup.spacing = 0f;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(skillsList.transform, false);
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        panel.skillsListContainer = content.GetComponent<RectTransform>();

        GameObject rowTemplateObject = new GameObject("SkillRowTemplate", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement), typeof(SkillRowUI));
        rowTemplateObject.transform.SetParent(content.transform, false);
        rowTemplateObject.GetComponent<LayoutElement>().preferredHeight = 60f;
        rowTemplateObject.GetComponent<HorizontalLayoutGroup>().spacing = 10f;
        SkillRowUI rowTemplate = rowTemplateObject.GetComponent<SkillRowUI>();
        rowTemplate.backgroundImage = rowTemplateObject.GetComponent<Image>();
        panel.skillRowTemplate = rowTemplate;

        return panelRoot;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float fontSize)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, out TextMeshProUGUI label, float labelFontSize)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        Button button = buttonObject.GetComponent<Button>();
        label = CreateText("Label", buttonObject.transform, labelFontSize);
        return button;
    }
}
