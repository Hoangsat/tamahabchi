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
    private Image heroCardBackgroundImage;
    private Image heroAccentImage;
    private TextMeshProUGUI heroCategoryText;
    private TextMeshProUGUI heroMetaText;
    private TextMeshProUGUI heroHintText;
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
        EnsurePanelLayering();

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
        gameManager.OnPetChanged -= RefreshUI;
        gameManager.OnPetFlowChanged -= RefreshUI;

        gameManager.OnCoinsChanged += RefreshUI;
        gameManager.OnInventoryChanged += RefreshUI;
        gameManager.OnProgressionChanged += RefreshUI;
        gameManager.OnPetChanged += RefreshUI;
        gameManager.OnPetFlowChanged += RefreshUI;
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
        gameManager.OnPetChanged -= RefreshUI;
        gameManager.OnPetFlowChanged -= RefreshUI;
    }

    private void RefreshUI()
    {
        BuildUiIfNeeded();
        EnsurePanelLayering();
        ApplyScreenVisuals();

        if (gameManager == null)
        {
            if (titleText != null)
            {
                titleText.text = "Shop";
            }

            if (coinsText != null)
            {
                coinsText.text = "Coins: --";
            }

            RefreshHeroBlock(null);
            SetStatus("GameManager missing");
            RebuildPlaceholder("Shop data unavailable");
            return;
        }

        if (titleText != null)
        {
            titleText.text = "Shop";
        }

        if (coinsText != null)
        {
            coinsText.text = $"Coins: {gameManager.GetCurrentCoins()}";
        }

        if (equippedSkinText != null)
        {
            equippedSkinText.text = $"Equipped skin: {FormatSkinLabel(gameManager.GetEquippedSkinId())}";
        }

        List<ShopItemViewData> items = gameManager.GetShopItems(selectedCategory);
        RefreshHeroBlock(items);
        if (items != null && items.Count > 0)
        {
            RebuildItems(items);
        }
        else
        {
            RebuildPlaceholder(gameManager.GetShopPlaceholderMessage(selectedCategory));
        }

        UpdateTabSelection();
    }

    private void RebuildItems(List<ShopItemViewData> items)
    {
        ClearSpawnedContent();

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

        RebuildLayouts();
    }

    private void RebuildPlaceholder(string message)
    {
        ClearSpawnedContent();

        if (emptyStateText != null)
        {
            emptyStateText.gameObject.SetActive(false);
        }

        GameObject card = CreateCard("PlaceholderCard");
        RectTransform cardRect = card.transform as RectTransform;
        cardRect.sizeDelta = new Vector2(0f, 220f);
        CreateAccentStrip(cardRect, "PlaceholderAccentStrip", GetCategoryAccent(selectedCategory), 10f);

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 28, 20);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CreateText(cardRect, "PlaceholderTitle", selectedCategory.ToString(), 34f, FontStyles.Bold, TextAlignmentOptions.Center);
        TextMeshProUGUI body = CreateText(cardRect, "PlaceholderBody", message, 24f, FontStyles.Normal, TextAlignmentOptions.Center);
        body.textWrappingMode = TextWrappingModes.Normal;

        spawnedContent.Add(card);
        RebuildLayouts();
    }

    private void CreateItemCard(ShopItemViewData item)
    {
        GameObject card = CreateCard("ShopItem_" + item.id);
        RectTransform cardRect = card.transform as RectTransform;
        LayoutElement cardLayout = card.AddComponent<LayoutElement>();
        cardLayout.preferredHeight = item.showUseAction ? 238f : 180f;
        CreateAccentStrip(cardRect, "CardAccentStrip", GetCategoryAccent(selectedCategory), 10f);

        VerticalLayoutGroup layout = card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 26, 18);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform headerRow = CreateObject("HeaderRow", cardRect);
        HorizontalLayoutGroup headerLayout = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = false;
        headerLayout.childControlHeight = false;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;

        GameObject iconBadge = CreateBadge(headerRow.transform as RectTransform, "IconBadge", item.iconLabel);
        LayoutElement iconLayout = iconBadge.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 84f;
        iconLayout.preferredHeight = 48f;

        TextMeshProUGUI title = CreateText(headerRow.transform as RectTransform, "Title", item.displayName, 30f, FontStyles.Bold, TextAlignmentOptions.Left);
        title.textWrappingMode = TextWrappingModes.NoWrap;

        TextMeshProUGUI description = CreateText(cardRect, "Description", item.description, 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        description.textWrappingMode = TextWrappingModes.Normal;
        description.color = new Color(0.85f, 0.9f, 0.96f, 0.95f);

        RectTransform footerRow = CreateObject("FooterRow", cardRect);
        HorizontalLayoutGroup footerLayout = footerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 12f;
        footerLayout.childAlignment = TextAnchor.MiddleLeft;
        footerLayout.childControlWidth = false;
        footerLayout.childControlHeight = false;
        footerLayout.childForceExpandWidth = false;
        footerLayout.childForceExpandHeight = false;

        TextMeshProUGUI priceText = CreateText(footerRow.transform as RectTransform, "PriceText", $"Price: {item.price}", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
        priceText.color = new Color(1f, 0.86f, 0.4f, 1f);

        TextMeshProUGUI ownedText = CreateText(footerRow.transform as RectTransform, "OwnedText", $"Owned: {item.ownedCount}", 22f, FontStyles.Normal, TextAlignmentOptions.Left);

        RectTransform actionRow = CreateObject("ActionRow", cardRect);
        HorizontalLayoutGroup actionLayout = actionRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 10f;
        actionLayout.childAlignment = TextAnchor.MiddleLeft;
        actionLayout.childControlWidth = false;
        actionLayout.childControlHeight = false;
        actionLayout.childForceExpandWidth = false;
        actionLayout.childForceExpandHeight = false;

        Button buyButton = CreateActionButton(actionRow.transform as RectTransform, "BuyButton", item.canBuy ? "Buy" : item.disabledReason, () => HandleBuy(item.id));
        buyButton.interactable = item.canBuy;
        LayoutElement buyLayout = buyButton.gameObject.AddComponent<LayoutElement>();
        buyLayout.preferredWidth = 220f;
        buyLayout.preferredHeight = 52f;
        Image buyImage = buyButton.GetComponent<Image>();
        if (buyImage != null)
        {
            buyImage.color = item.canBuy
                ? Color.Lerp(new Color(0.22f, 0.36f, 0.58f, 1f), GetCategoryAccent(selectedCategory), 0.3f)
                : new Color(0.22f, 0.24f, 0.3f, 1f);
        }

        if (item.showUseAction)
        {
            Button useButton = CreateActionButton(actionRow.transform as RectTransform, "UseButton", item.useActionLabel, () => HandleUseOrEquip(item));
            useButton.interactable = item.canUseAction;
            LayoutElement useLayout = useButton.gameObject.AddComponent<LayoutElement>();
            useLayout.preferredWidth = 220f;
            useLayout.preferredHeight = 52f;
            Image useImage = useButton.GetComponent<Image>();
            if (useImage != null)
            {
                useImage.color = item.canUseAction
                    ? Color.Lerp(new Color(0.20f, 0.46f, 0.32f, 0.98f), GetCategoryAccent(selectedCategory), 0.18f)
                    : new Color(0.18f, 0.25f, 0.21f, 1f);
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

    private void UpdateTabSelection()
    {
        foreach (KeyValuePair<ShopCategory, Button> entry in tabButtons)
        {
            Image image = entry.Value != null ? entry.Value.GetComponent<Image>() : null;
            if (image != null)
            {
                image.color = entry.Key == selectedCategory
                    ? new Color(0.31f, 0.57f, 0.92f, 1f)
                    : new Color(0.18f, 0.22f, 0.3f, 1f);
            }

            if (tabButtonLabels.TryGetValue(entry.Key, out TextMeshProUGUI label) && label != null)
            {
                label.color = entry.Key == selectedCategory
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(0.86f, 0.9f, 0.96f, 0.94f);
            }
        }
    }

    private void ClearSpawnedContent()
    {
        for (int i = 0; i < spawnedContent.Count; i++)
        {
            if (spawnedContent[i] != null)
            {
                Destroy(spawnedContent[i]);
            }
        }

        spawnedContent.Clear();
    }

    private void RebuildLayouts()
    {
        if (scrollContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
        }

        if (screenRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(screenRoot);
        }
    }

    private void ApplyScreenVisuals()
    {
        if (statusText != null && string.IsNullOrEmpty(statusText.text))
        {
            statusText.text = gameManager != null ? gameManager.GetShopCategoryStatus(selectedCategory) : "Shop is ready.";
        }
    }

    private void RefreshHeroBlock(List<ShopItemViewData> items)
    {
        Color accent = GetCategoryAccent(selectedCategory);

        if (heroCardBackgroundImage != null)
        {
            heroCardBackgroundImage.color = Color.Lerp(new Color(0.14f, 0.18f, 0.27f, 0.98f), accent, 0.25f);
        }

        if (heroAccentImage != null)
        {
            heroAccentImage.color = accent;
        }

        if (heroCategoryText != null)
        {
            heroCategoryText.text = $"{selectedCategory} Loadout";
        }

        if (heroMetaText != null)
        {
            heroMetaText.text = BuildHeroMeta(items);
        }

        if (heroHintText != null)
        {
            heroHintText.text = BuildHeroHint(items);
        }
    }

    private string BuildHeroMeta(List<ShopItemViewData> items)
    {
        if (gameManager == null)
        {
            return "Store data unavailable";
        }

        int coins = gameManager.GetCurrentCoins();
        int readyCount = 0;
        int itemCount = items != null ? items.Count : 0;

        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].canBuy)
                {
                    readyCount++;
                }
            }
        }

        return $"{itemCount} items | {readyCount} ready | {coins} coins";
    }

    private string BuildHeroHint(List<ShopItemViewData> items)
    {
        if (selectedCategory == ShopCategory.Special)
        {
            return "Special stays as a future slot. We can fill it with premium or event content later.";
        }

        if (selectedCategory == ShopCategory.Skins)
        {
            return "Pick a cosmetic, buy it once, then equip it from the same card.";
        }

        if (items == null || items.Count == 0)
        {
            return "This category is scaffolded. We can flesh it out once the feature is ready.";
        }

        return "Use the cards below to buy, stock up, and trigger quick upgrades for your pet loop.";
    }

    private Color GetCategoryAccent(ShopCategory category)
    {
        switch (category)
        {
            case ShopCategory.Food:
                return new Color(0.96f, 0.71f, 0.34f, 1f);
            case ShopCategory.Energy:
                return new Color(0.44f, 0.81f, 0.58f, 1f);
            case ShopCategory.Mood:
                return new Color(0.91f, 0.48f, 0.84f, 1f);
            case ShopCategory.Skins:
                return new Color(0.72f, 0.52f, 0.95f, 1f);
            case ShopCategory.Special:
                return new Color(0.38f, 0.69f, 0.95f, 1f);
            default:
                return new Color(0.32f, 0.59f, 0.94f, 1f);
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = string.IsNullOrEmpty(message)
                ? (gameManager != null ? gameManager.GetShopCategoryStatus(selectedCategory) : "Shop is ready.")
                : message;
        }
    }

    private void EnsurePanelLayering()
    {
        if (panelRoot == null)
        {
            return;
        }

        Transform shellRuntimeRoot = transform.Find("ShellRuntimeRoot");
        if (shellRuntimeRoot == null)
        {
            return;
        }

        panelRoot.transform.SetSiblingIndex(shellRuntimeRoot.GetSiblingIndex());
    }

    private void BuildUiIfNeeded()
    {
        if (panelRoot != null)
        {
            return;
        }

        RectTransform canvasRect = transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        panelRoot = CreatePanel(canvasRect, "ShopPanelRoot", new Color(0.05f, 0.09f, 0.14f, 0.96f));
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = new Vector2(0f, 146f);
        panelRect.offsetMax = Vector2.zero;

        screenRoot = CreateObject("ShopScreenRoot", panelRect);
        screenRoot.anchorMin = Vector2.zero;
        screenRoot.anchorMax = Vector2.one;
        screenRoot.offsetMin = new Vector2(28f, 24f);
        screenRoot.offsetMax = new Vector2(-28f, -24f);

        VerticalLayoutGroup rootLayout = screenRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(0, 0, 0, 0);
        rootLayout.spacing = 18f;
        rootLayout.childAlignment = TextAnchor.UpperCenter;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = false;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateObject("HeaderRow", screenRoot);
        HorizontalLayoutGroup headerLayout = headerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.spacing = 12f;
        headerLayout.childAlignment = TextAnchor.MiddleCenter;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = false;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;
        LayoutElement headerElement = headerRow.gameObject.AddComponent<LayoutElement>();
        headerElement.preferredHeight = 64f;

        titleText = CreateText(headerRow.transform as RectTransform, "TitleText", "Shop", 42f, FontStyles.Bold, TextAlignmentOptions.Left);
        LayoutElement titleLayout = titleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredWidth = 220f;

        coinsText = CreateText(headerRow.transform as RectTransform, "CoinsText", "Coins: 0", 26f, FontStyles.Bold, TextAlignmentOptions.Center);
        LayoutElement coinsLayout = coinsText.gameObject.AddComponent<LayoutElement>();
        coinsLayout.preferredWidth = 240f;

        closeButton = CreateActionButton(headerRow.transform as RectTransform, "CloseButton", "Back", ClosePanel);
        LayoutElement closeLayout = closeButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 160f;
        closeLayout.preferredHeight = 52f;

        RectTransform heroCard = CreatePanel(screenRoot, "HeroCard", new Color(0.14f, 0.18f, 0.27f, 0.98f)).GetComponent<RectTransform>();
        LayoutElement heroLayout = heroCard.gameObject.AddComponent<LayoutElement>();
        heroLayout.preferredHeight = 118f;
        VerticalLayoutGroup heroCardLayout = heroCard.gameObject.AddComponent<VerticalLayoutGroup>();
        heroCardLayout.padding = new RectOffset(18, 18, 16, 16);
        heroCardLayout.spacing = 6f;
        heroCardLayout.childAlignment = TextAnchor.UpperLeft;
        heroCardLayout.childControlWidth = true;
        heroCardLayout.childControlHeight = false;
        heroCardLayout.childForceExpandWidth = true;
        heroCardLayout.childForceExpandHeight = false;

        heroCardBackgroundImage = heroCard.GetComponent<Image>();
        heroAccentImage = CreateAccentStrip(heroCard, "HeroAccentStrip", GetCategoryAccent(ShopCategory.Food), 12f);
        heroCategoryText = CreateText(heroCard, "HeroCategoryText", "Food Loadout", 30f, FontStyles.Bold, TextAlignmentOptions.Left);
        heroMetaText = CreateText(heroCard, "HeroMetaText", "0 items | 0 ready | 0 coins", 20f, FontStyles.Bold, TextAlignmentOptions.Left);
        heroMetaText.color = new Color(1f, 0.88f, 0.48f, 1f);
        heroHintText = CreateText(heroCard, "HeroHintText", "Use the cards below to buy, stock up, and trigger quick upgrades for your pet loop.", 19f, FontStyles.Normal, TextAlignmentOptions.Left);
        heroHintText.textWrappingMode = TextWrappingModes.Normal;
        heroHintText.color = new Color(0.86f, 0.9f, 0.96f, 0.95f);

        tabRow = CreateObject("TabRow", screenRoot);
        HorizontalLayoutGroup tabLayout = tabRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 10f;
        tabLayout.childAlignment = TextAnchor.MiddleCenter;
        tabLayout.childControlWidth = true;
        tabLayout.childControlHeight = true;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = false;
        LayoutElement tabRowLayout = tabRow.gameObject.AddComponent<LayoutElement>();
        tabRowLayout.preferredHeight = 60f;

        CreateTabButton(tabRow, ShopCategory.Food, "Food");
        CreateTabButton(tabRow, ShopCategory.Energy, "Energy");
        CreateTabButton(tabRow, ShopCategory.Mood, "Mood");
        CreateTabButton(tabRow, ShopCategory.Skins, "Skins");
        CreateTabButton(tabRow, ShopCategory.Special, "Special");

        statusText = CreateText(screenRoot, "StatusText", "Food is fully active in this build.", 22f, FontStyles.Normal, TextAlignmentOptions.Left);
        statusText.textWrappingMode = TextWrappingModes.Normal;
        statusText.color = new Color(0.83f, 0.89f, 0.96f, 0.96f);

        equippedSkinText = CreateText(screenRoot, "EquippedSkinText", "Equipped skin: Default", 20f, FontStyles.Normal, TextAlignmentOptions.Left);
        equippedSkinText.color = new Color(0.73f, 0.86f, 0.98f, 0.92f);

        GameObject scrollView = CreatePanel(screenRoot, "ScrollView", new Color(0.09f, 0.12f, 0.18f, 0.7f));
        LayoutElement scrollLayout = scrollView.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.preferredHeight = 600f;

        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreatePanel(scrollView.transform as RectTransform, "Viewport", new Color(0f, 0f, 0f, 0f));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        scrollContent = CreateObject("Content", viewportRect);
        scrollContent.anchorMin = new Vector2(0f, 1f);
        scrollContent.anchorMax = new Vector2(1f, 1f);
        scrollContent.pivot = new Vector2(0.5f, 1f);
        scrollContent.anchoredPosition = Vector2.zero;
        scrollContent.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(20, 20, 20, 20);
        contentLayout.spacing = 14f;
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = scrollContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportRect;
        scrollRect.content = scrollContent;

        emptyStateText = CreateText(scrollContent, "EmptyStateText", string.Empty, 22f, FontStyles.Normal, TextAlignmentOptions.Center);
        emptyStateText.gameObject.SetActive(false);

        EnsurePanelLayering();
    }

    private void CreateTabButton(RectTransform parent, ShopCategory category, string label)
    {
        Button button = CreateActionButton(parent, category + "TabButton", label, () => SetCategory(category));
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 54f;
        tabButtons[category] = button;

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
        {
            tabButtonLabels[category] = labelText;
        }
    }

    private GameObject CreateCard(string name)
    {
        return CreatePanel(scrollContent, name, Color.Lerp(new Color(0.15f, 0.2f, 0.28f, 0.96f), GetCategoryAccent(selectedCategory), 0.12f));
    }

    private Image CreateAccentStrip(RectTransform parent, string name, Color color, float height)
    {
        GameObject strip = CreatePanel(parent, name, color);
        RectTransform stripRect = strip.GetComponent<RectTransform>();
        stripRect.anchorMin = new Vector2(0f, 1f);
        stripRect.anchorMax = new Vector2(1f, 1f);
        stripRect.pivot = new Vector2(0.5f, 1f);
        stripRect.offsetMin = new Vector2(0f, -height);
        stripRect.offsetMax = Vector2.zero;
        return strip.GetComponent<Image>();
    }

    private GameObject CreateBadge(RectTransform parent, string name, string value)
    {
        GameObject badge = CreatePanel(parent, name, new Color(0.25f, 0.34f, 0.48f, 1f));
        TextMeshProUGUI text = CreateText(badge.transform as RectTransform, "Label", string.IsNullOrEmpty(value) ? "ITEM" : value, 18f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return badge;
    }

    private GameObject CreatePanel(RectTransform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private RectTransform CreateObject(string name, RectTransform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, string value, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment)
    {
        RectTransform textRoot = CreateObject(name, parent);
        TextMeshProUGUI text = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = new Color(0.94f, 0.97f, 1f, 1f);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private Button CreateActionButton(RectTransform parent, string name, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreatePanel(parent, name, new Color(0.22f, 0.36f, 0.58f, 1f));
        Button button = buttonObject.AddComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText(buttonObject.transform as RectTransform, "Label", label, 20f, FontStyles.Bold, TextAlignmentOptions.Center);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        text.textWrappingMode = TextWrappingModes.Normal;
        return button;
    }

    private string FormatSkinLabel(string skinId)
    {
        if (string.IsNullOrEmpty(skinId) || string.Equals(skinId, "default"))
        {
            return "Default";
        }

        string trimmed = skinId.StartsWith("skin_") ? skinId.Substring(5) : skinId;
        string[] parts = trimmed.Split('_');
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i]))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(char.ToUpper(parts[i][0]));
            if (parts[i].Length > 1)
            {
                builder.Append(parts[i].Substring(1));
            }
        }

        return builder.Length > 0 ? builder.ToString() : "Default";
    }
}
