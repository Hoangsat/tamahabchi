using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelUI : MonoBehaviour
{
    public GameObject panelRoot;
    public Button closeButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI coinsText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI emptyStateText;
    public TextMeshProUGUI equippedSkinText;

    private readonly List<GameObject> spawnedContent = new List<GameObject>();
    private readonly Dictionary<ShopCategory, Button> tabButtons = new Dictionary<ShopCategory, Button>();
    private readonly Dictionary<ShopCategory, TextMeshProUGUI> tabButtonLabels = new Dictionary<ShopCategory, TextMeshProUGUI>();

    private GameManager gameManager;
    private ShopCategory selectedCategory = ShopCategory.Food;

    private RectTransform screenRoot;
    private RectTransform tabRow;
    private RectTransform scrollContent;

    private void Awake()
    {
        BuildUiIfNeeded();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
            closeButton.onClick.AddListener(ClosePanel);
            closeButton.gameObject.SetActive(false);
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        AttachEvents();
    }

    private void OnDisable()
    {
        DetachEvents();
    }

    public void SetGameManager(GameManager manager)
    {
        if (gameManager == manager)
        {
            return;
        }

        DetachEvents();
        gameManager = manager;
        AttachEvents();
        RefreshUI();
    }

    public void ShowPanel()
    {
        BuildUiIfNeeded();
        ShopPanelViewUtility.EnsurePanelLayering(panelRoot, transform);

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(false);
        }

        SetStatus(string.Empty);
        selectedCategory = ShopCategory.Food;
        RefreshUI();
    }

    public void HidePanel()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        SetStatus(string.Empty);
    }

    public bool IsPanelVisible()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }

    private void ClosePanel()
    {
        HidePanel();
    }

    private void AttachEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnCoinsChanged -= RefreshUI;
        gameManager.OnInventoryChanged -= RefreshUI;
        gameManager.OnProgressionChanged -= RefreshUI;
        gameManager.OnPetChanged -= RefreshPetSummary;
        gameManager.OnPetFlowChanged -= RefreshPetSummary;

        gameManager.OnCoinsChanged += RefreshUI;
        gameManager.OnInventoryChanged += RefreshUI;
        gameManager.OnProgressionChanged += RefreshUI;
        gameManager.OnPetChanged += RefreshPetSummary;
        gameManager.OnPetFlowChanged += RefreshPetSummary;
    }

    private void DetachEvents()
    {
        if (gameManager == null)
        {
            return;
        }

        gameManager.OnCoinsChanged -= RefreshUI;
        gameManager.OnInventoryChanged -= RefreshUI;
        gameManager.OnProgressionChanged -= RefreshUI;
        gameManager.OnPetChanged -= RefreshPetSummary;
        gameManager.OnPetFlowChanged -= RefreshPetSummary;
    }

    private void RefreshPetSummary()
    {
        if (!IsPanelVisible())
        {
            return;
        }

        SetStatus(string.Empty);
    }

    private void RefreshUI()
    {
        if (panelRoot != null && !panelRoot.activeSelf)
        {
            return;
        }

        BuildUiIfNeeded();
        ShopPanelViewUtility.EnsurePanelLayering(panelRoot, transform);
        ApplyScreenVisuals();

        if (gameManager == null)
        {
            if (titleText != null)
            {
                titleText.text = ShopPanelPresenter.BuildScreenTitle(selectedCategory);
            }

            if (coinsText != null)
            {
                coinsText.text = "Coins: --";
            }

            if (equippedSkinText != null)
            {
                equippedSkinText.text = "Shop summary unavailable";
            }

            SetStatus("GameManager missing");
            RebuildPlaceholder("Shop data unavailable");
            return;
        }

        if (titleText != null)
        {
            titleText.text = ShopPanelPresenter.BuildScreenTitle(selectedCategory);
        }

        if (coinsText != null)
        {
            coinsText.text = ShopPanelPresenter.BuildCoinsLabel(gameManager.GetCurrentCoins());
        }

        List<ShopItemViewData> items = gameManager.GetShopItems(selectedCategory);
        RefreshCategorySummary(items);
        if (items != null && items.Count > 0)
        {
            RebuildItems(items);
        }
        else
        {
            RebuildPlaceholder(gameManager.GetShopPlaceholderMessage(selectedCategory));
        }

        ShopPanelViewUtility.UpdateTabSelection(tabButtons, tabButtonLabels, selectedCategory);
    }

    private void RebuildItems(List<ShopItemViewData> items)
    {
        ShopPanelViewUtility.ClearSpawnedContent(scrollContent, emptyStateText, spawnedContent);

        if (items == null || items.Count == 0)
        {
            RebuildPlaceholder("No items available");
            return;
        }

        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(false);
        }

        for (int i = 0; i < items.Count; i++)
        {
            CreateItemCard(items[i]);
        }

        ShopPanelViewUtility.RebuildLayouts(scrollContent, screenRoot);
    }

    private void RebuildPlaceholder(string message)
    {
        ShopPanelViewUtility.ClearSpawnedContent(scrollContent, emptyStateText, spawnedContent);

        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(false);
        }

        GameObject card = ShopPanelViewUtility.CreatePanel(scrollContent, "PlaceholderCard", new Color(0.15f, 0.2f, 0.28f, 0.96f));
        RectTransform cardRect = card.transform as RectTransform;
        cardRect.sizeDelta = new Vector2(0f, 220f);

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ShopPanelViewUtility.CreateText(cardRect, "PlaceholderTitle", ShopPanelPresenter.GetCategoryLabel(selectedCategory), 34f, FontStyles.Bold, TextAlignmentOptions.Center);
        TextMeshProUGUI body = ShopPanelViewUtility.CreateText(cardRect, "PlaceholderBody", message, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        body.textWrappingMode = TextWrappingModes.Normal;

        spawnedContent.Add(card);
        ShopPanelViewUtility.RebuildLayouts(scrollContent, screenRoot);
    }

    private void CreateItemCard(ShopItemViewData item)
    {
        GameObject card = ShopPanelViewUtility.CreatePanel(scrollContent, "ShopItem_" + item.id, new Color(0.15f, 0.2f, 0.28f, 0.96f));
        RectTransform cardRect = card.transform as RectTransform;
        LayoutElement cardLayout = card.AddComponent<LayoutElement>();
        cardLayout.preferredHeight = item.showUseAction ? 210f : 156f;

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 18, 18);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform headerRow = ShopPanelViewUtility.CreateObject("HeaderRow", cardRect);
        HorizontalLayoutGroup headerLayout = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = false;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        LayoutElement headerRowLayout = headerRow.gameObject.AddComponent<LayoutElement>();
        headerRowLayout.preferredHeight = 48f;

        GameObject iconBadge = ShopPanelViewUtility.CreateBadge(headerRow, "IconBadge", item.iconLabel);
        LayoutElement iconLayout = iconBadge.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 84f;
        iconLayout.preferredHeight = 40f;

        TextMeshProUGUI title = ShopPanelViewUtility.CreateText(headerRow, "Title", item.displayName, 30f, FontStyles.Bold, TextAlignmentOptions.Left);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        LayoutElement titleElement = title.gameObject.AddComponent<LayoutElement>();
        titleElement.flexibleWidth = 1f;
        titleElement.preferredHeight = 40f;

        TextMeshProUGUI description = ShopPanelViewUtility.CreateText(cardRect, "Description", item.description, 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        description.textWrappingMode = TextWrappingModes.Normal;
        description.color = new Color(0.85f, 0.9f, 0.96f, 0.95f);
        LayoutElement descriptionLayout = description.gameObject.AddComponent<LayoutElement>();
        descriptionLayout.preferredHeight = 34f;

        RectTransform footerRow = ShopPanelViewUtility.CreateObject("FooterRow", cardRect);
        HorizontalLayoutGroup footerLayout = footerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 12f;
        footerLayout.childAlignment = TextAnchor.MiddleLeft;
        footerLayout.childControlWidth = false;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = false;
        footerLayout.childForceExpandHeight = false;
        LayoutElement footerRowLayout = footerRow.gameObject.AddComponent<LayoutElement>();
        footerRowLayout.preferredHeight = 28f;

        TextMeshProUGUI priceText = ShopPanelViewUtility.CreateText(
            footerRow,
            "PriceText",
            ShopPanelPresenter.BuildPriceLabel(item),
            22f,
            FontStyles.Bold,
            TextAlignmentOptions.Left);
        priceText.color = new Color(1f, 0.86f, 0.4f, 1f);
        LayoutElement priceLayout = priceText.gameObject.AddComponent<LayoutElement>();
        priceLayout.preferredHeight = 28f;

        TextMeshProUGUI ownedText = ShopPanelViewUtility.CreateText(
            footerRow,
            "OwnedText",
            ShopPanelPresenter.BuildOwnedLabel(item),
            22f,
            FontStyles.Normal,
            TextAlignmentOptions.Left);
        LayoutElement ownedLayout = ownedText.gameObject.AddComponent<LayoutElement>();
        ownedLayout.preferredHeight = 28f;

        RectTransform actionRow = ShopPanelViewUtility.CreateObject("ActionRow", cardRect);
        HorizontalLayoutGroup actionLayout = actionRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 10f;
        actionLayout.childAlignment = TextAnchor.MiddleLeft;
        actionLayout.childControlWidth = true;
        actionLayout.childControlHeight = true;
        actionLayout.childForceExpandWidth = false;
        actionLayout.childForceExpandHeight = false;
        LayoutElement actionRowLayout = actionRow.gameObject.AddComponent<LayoutElement>();
        actionRowLayout.preferredHeight = 52f;

        Button buyButton = ShopPanelViewUtility.CreateActionButton(actionRow, "BuyButton", item.canBuy ? "Buy" : item.disabledReason, () => HandleBuy(item.id));
        buyButton.interactable = item.canBuy;
        LayoutElement buyLayout = buyButton.gameObject.AddComponent<LayoutElement>();
        buyLayout.preferredWidth = 220f;
        buyLayout.preferredHeight = 52f;
        if (!item.canBuy)
        {
            Image buyImage = buyButton.GetComponent<Image>();
            if (buyImage != null)
            {
                buyImage.color = new Color(0.22f, 0.24f, 0.3f, 1f);
            }
        }

        if (item.showUseAction)
        {
            Button useButton = ShopPanelViewUtility.CreateActionButton(actionRow, "UseButton", item.useActionLabel, () => HandleUseOrEquip(item));
            useButton.interactable = item.canUseAction;
            LayoutElement useLayout = useButton.gameObject.AddComponent<LayoutElement>();
            useLayout.preferredWidth = 220f;
            useLayout.preferredHeight = 52f;
            if (!item.canUseAction)
            {
                Image useImage = useButton.GetComponent<Image>();
                if (useImage != null)
                {
                    useImage.color = new Color(0.18f, 0.25f, 0.21f, 1f);
                }
            }
        }

        spawnedContent.Add(card);
    }

    private void HandleBuy(string itemId)
    {
        if (gameManager == null)
        {
            return;
        }

        bool success = gameManager.TryPurchaseShopItem(itemId, out string message);
        SetStatus(message);
        if (success)
        {
            RefreshUI();
        }
    }

    private void HandleUseOrEquip(ShopItemViewData item)
    {
        if (gameManager == null || item == null)
        {
            return;
        }

        bool success;
        string message;
        if (item.kind == ShopItemKind.Cosmetic)
        {
            success = gameManager.TryEquipShopSkin(item.id, out message);
        }
        else
        {
            success = gameManager.TryUseShopItem(item.id, out message);
        }

        SetStatus(message);
        if (success)
        {
            RefreshUI();
        }
    }

    private void SetCategory(ShopCategory category)
    {
        if (selectedCategory == category)
        {
            return;
        }

        selectedCategory = category;
        SetStatus(string.Empty);
        RefreshUI();
    }

    private void RefreshCategorySummary(List<ShopItemViewData> items)
    {
        if (equippedSkinText == null)
        {
            return;
        }

        if (gameManager == null)
        {
            equippedSkinText.text = "Shop summary unavailable";
            equippedSkinText.gameObject.SetActive(true);
            return;
        }

        equippedSkinText.text = ShopPanelPresenter.BuildCategorySummary(selectedCategory, items, gameManager.GetEquippedSkinId());
        equippedSkinText.gameObject.SetActive(true);
    }

    private void BuildUiIfNeeded()
    {
        if (panelRoot != null)
        {
            ShopPanelLayoutRefs resolved = ShopPanelLayoutBuilder.ResolveExisting(panelRoot);
            if (resolved.HasCoreReferences())
            {
                ApplyLayoutRefs(resolved);
                return;
            }
        }

        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        ApplyLayoutRefs(ShopPanelLayoutBuilder.BuildLayout(canvasRect, ClosePanel, SetCategory));
        ShopPanelViewUtility.EnsurePanelLayering(panelRoot, transform);
    }

    private void ApplyScreenVisuals()
    {
        if (statusText != null && string.IsNullOrEmpty(statusText.text))
        {
            statusText.text = GetStatusText();
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = GetStatusText(message);
        }
    }

    private string GetStatusText(string messageOverride = null)
    {
        string categoryStatus = gameManager != null
            ? gameManager.GetShopCategoryStatus(selectedCategory)
            : "Shop is ready.";
        string petSummary = gameManager != null ? gameManager.GetPetVitalsSummaryText() : string.Empty;
        return ShopPanelPresenter.BuildStatusText(petSummary, categoryStatus, messageOverride);
    }

    private void ApplyLayoutRefs(ShopPanelLayoutRefs refs)
    {
        if (refs == null)
        {
            return;
        }

        panelRoot = refs.PanelRoot;
        closeButton = refs.CloseButton;
        titleText = refs.TitleText;
        coinsText = refs.CoinsText;
        statusText = refs.StatusText;
        emptyStateText = refs.EmptyStateText;
        equippedSkinText = refs.EquippedSkinText;
        screenRoot = refs.ScreenRoot;
        tabRow = refs.TabRow;
        scrollContent = refs.ScrollContent;

        tabButtons.Clear();
        foreach (KeyValuePair<ShopCategory, Button> entry in refs.TabButtons)
        {
            tabButtons[entry.Key] = entry.Value;
        }

        tabButtonLabels.Clear();
        foreach (KeyValuePair<ShopCategory, TextMeshProUGUI> entry in refs.TabButtonLabels)
        {
            tabButtonLabels[entry.Key] = entry.Value;
        }
    }
}
