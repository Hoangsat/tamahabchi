using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class MissionPanelLayoutRefs
{
    public RectTransform ScreenRoot;
    public TextMeshProUGUI TitleText;
    public Button CloseButton;
    public TextMeshProUGUI ResetInfoText;
    public TextMeshProUGUI HeaderStatsText;
    public TextMeshProUGUI PanelStatusText;
    public RectTransform ScrollContent;
    public TextMeshProUGUI EmptyStateText;
    public Button FooterCreateButton;
    public Button FooterRerollButton;
}

public static class MissionPanelLayoutBuilder
{
    public static MissionPanelLayoutRefs Build(RectTransform panelRoot, UnityAction onClose, UnityAction onOpenCreate, UnityAction onReroll)
    {
        MissionPanelLayoutRefs refs = new MissionPanelLayoutRefs();
        if (panelRoot == null)
        {
            return refs;
        }

        RectTransform screenRoot = MissionPanelViewUtility.CreateObject("MissionScreenRoot", panelRoot);
        screenRoot.anchorMin = Vector2.zero;
        screenRoot.anchorMax = Vector2.one;
        screenRoot.offsetMin = new Vector2(24f, 132f);
        screenRoot.offsetMax = new Vector2(-24f, -24f);
        screenRoot.pivot = new Vector2(0.5f, 0.5f);
        refs.ScreenRoot = screenRoot;

        RectTransform headerBlock = MissionPanelViewUtility.CreateObject("HeaderBlock", screenRoot);
        RectTransform headerRect = headerBlock;
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(0f, -148f);
        headerRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup headerLayout = headerBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        headerLayout.spacing = 10f;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = true;
        headerLayout.childForceExpandHeight = false;
        ContentSizeFitter headerFitter = headerBlock.gameObject.AddComponent<ContentSizeFitter>();
        headerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform topRow = MissionPanelViewUtility.CreateObject("HeaderTopRow", headerBlock);
        HorizontalLayoutGroup topRowLayout = topRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        topRowLayout.spacing = 12f;
        topRowLayout.childControlWidth = true;
        topRowLayout.childControlHeight = true;
        topRowLayout.childForceExpandWidth = false;
        topRowLayout.childForceExpandHeight = false;
        topRowLayout.childAlignment = TextAnchor.MiddleCenter;
        LayoutElement topRowLayoutElement = topRow.gameObject.AddComponent<LayoutElement>();
        topRowLayoutElement.preferredHeight = 56f;

        refs.TitleText = MissionPanelViewUtility.CreateText(topRow, "TitleText", "Missions", 42f, FontStyles.Bold, TextAlignmentOptions.Left);
        LayoutElement titleLayout = refs.TitleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;
        refs.TitleText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.TitleText.overflowMode = TextOverflowModes.Overflow;

        refs.CloseButton = MissionPanelViewUtility.CreateActionButton(topRow, "CloseButton", "Close", onClose);
        MissionPanelViewUtility.ApplyButtonSizing(refs.CloseButton, 160f, 56f, false);

        refs.ResetInfoText = MissionPanelViewUtility.CreateText(headerBlock, "ResetInfoText", string.Empty, 20f, FontStyles.Normal, TextAlignmentOptions.Left);
        refs.HeaderStatsText = MissionPanelViewUtility.CreateText(headerBlock, "HeaderStatsText", string.Empty, 18f, FontStyles.Bold, TextAlignmentOptions.Left);
        refs.PanelStatusText = MissionPanelViewUtility.CreateText(headerBlock, "PanelStatusText", string.Empty, 18f, FontStyles.Normal, TextAlignmentOptions.Left);
        refs.PanelStatusText.color = new Color(0.84f, 0.9f, 0.98f, 0.9f);

        refs.ScrollContent = MissionPanelViewUtility.CreateScrollContent(screenRoot, "MissionScroll");
        RectTransform scrollRoot = refs.ScrollContent.parent != null ? refs.ScrollContent.parent.parent as RectTransform : null;
        if (scrollRoot != null)
        {
            scrollRoot.anchorMin = new Vector2(0f, 0f);
            scrollRoot.anchorMax = new Vector2(1f, 1f);
            scrollRoot.offsetMin = new Vector2(0f, 92f);
            scrollRoot.offsetMax = new Vector2(0f, -172f);
            scrollRoot.pivot = new Vector2(0.5f, 0.5f);
        }

        refs.EmptyStateText = MissionPanelViewUtility.CreateText(screenRoot, "EmptyStateText", string.Empty, 24f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform emptyStateRect = refs.EmptyStateText.rectTransform;
        emptyStateRect.anchorMin = new Vector2(0f, 0f);
        emptyStateRect.anchorMax = new Vector2(1f, 1f);
        emptyStateRect.offsetMin = new Vector2(48f, 120f);
        emptyStateRect.offsetMax = new Vector2(-48f, -188f);

        RectTransform footer = MissionPanelViewUtility.CreateObject("FooterActions", screenRoot);
        RectTransform footerRect = footer;
        footerRect.anchorMin = new Vector2(0f, 0f);
        footerRect.anchorMax = new Vector2(1f, 0f);
        footerRect.pivot = new Vector2(0.5f, 0f);
        footerRect.offsetMin = Vector2.zero;
        footerRect.offsetMax = new Vector2(0f, 72f);

        HorizontalLayoutGroup footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 14f;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = true;
        footerLayout.childForceExpandHeight = false;

        refs.FooterCreateButton = MissionPanelViewUtility.CreateActionButton(footer, "CreateButton", "Create", onOpenCreate);
        refs.FooterRerollButton = MissionPanelViewUtility.CreateActionButton(footer, "RerollButton", "Reroll", onReroll);
        MissionPanelViewUtility.ApplyButtonSizing(refs.FooterCreateButton, 0f, 64f, true);
        MissionPanelViewUtility.ApplyButtonSizing(refs.FooterRerollButton, 0f, 64f, true);
        refs.FooterRerollButton.interactable = false;

        return refs;
    }
}
