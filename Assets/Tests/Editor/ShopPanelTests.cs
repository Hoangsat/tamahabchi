using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelTests
{
    [Test]
    public void ShowPanel_BuildsScrollableLayoutForMobile()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            ShopPanelUI panel = canvasObject.AddComponent<ShopPanelUI>();
            panel.ShowPanel();

            Assert.NotNull(panel.panelRoot);

            Transform screenRoot = panel.panelRoot.transform.Find("ShopScreenRoot");
            Assert.NotNull(screenRoot);

            Transform scrollView = screenRoot.Find("ScrollView");
            Assert.NotNull(scrollView);
            Assert.NotNull(scrollView.GetComponent<ScrollRect>());
            Assert.NotNull(scrollView.Find("Viewport"));
            Assert.NotNull(scrollView.Find("Viewport").GetComponent<RectMask2D>());
            Assert.NotNull(scrollView.Find("Viewport/ContentHost"));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void BuyItem_WhenUnlockedAndAffordable_AddsInventoryAndSpendsCoins()
    {
        BalanceConfig balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();

        PetData petData = new PetData { hunger = 50f, mood = 50f, energy = 50f, hasIndependentStats = true };
        CurrencyData currencyData = new CurrencyData { coins = 25 };
        InventoryData inventoryData = new InventoryData();
        ProgressionData progressionData = new ProgressionData { level = 2, xp = 0 };
        RoomData roomData = new RoomData();
        OnboardingData onboardingData = new OnboardingData();

        CurrencySystem currencySystem = new CurrencySystem(currencyData);
        InventorySystem inventorySystem = new InventorySystem(inventoryData);
        ShopSystem shopSystem = new ShopSystem(currencySystem, inventorySystem);
        ProgressionSystem progressionSystem = new ProgressionSystem(progressionData, balanceConfig.xpToNextLevel, 0);
        MissionSystem missionSystem = new MissionSystem();
        missionSystem.Init(new MissionData());

        int coinsChangedCount = 0;
        int inventoryChangedCount = 0;
        int xpAwarded = 0;

        CoreLoopActionCoordinator coordinator = new CoreLoopActionCoordinator(
            petData,
            currencyData,
            progressionData,
            roomData,
            onboardingData,
            new PetSystem(petData),
            currencySystem,
            inventorySystem,
            shopSystem,
            progressionSystem,
            missionSystem,
            balanceConfig,
            2,
            new CoreLoopActionCoordinatorCallbacks
            {
                OnCoinsChanged = () => coinsChangedCount++,
                OnInventoryChanged = () => inventoryChangedCount++
            });

        ShopPurchaseResult result = coordinator.TryBuyItem("food_basic", 10);

        Assert.True(result.success);
        Assert.AreEqual("Bought Basic", result.message);
        Assert.AreEqual(15, currencyData.coins);
        Assert.AreEqual(1, inventorySystem.GetItemCount("food_basic"));
        Assert.AreEqual(1, coinsChangedCount);
        Assert.AreEqual(1, inventoryChangedCount);
        Assert.AreEqual(0, xpAwarded);
        Assert.True(onboardingData.didBuyFood);
    }

    [Test]
    public void BuyItem_WhenNotAffordable_ReturnsNeedCoinsAndLeavesStateUnchanged()
    {
        BalanceConfig balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();

        PetData petData = new PetData { hunger = 50f, mood = 50f, energy = 50f, hasIndependentStats = true };
        CurrencyData currencyData = new CurrencyData { coins = 5 };
        InventoryData inventoryData = new InventoryData();
        ProgressionData progressionData = new ProgressionData { level = 1, xp = 0 };

        CurrencySystem currencySystem = new CurrencySystem(currencyData);
        InventorySystem inventorySystem = new InventorySystem(inventoryData);
        ShopSystem shopSystem = new ShopSystem(currencySystem, inventorySystem);
        ProgressionSystem progressionSystem = new ProgressionSystem(progressionData, balanceConfig.xpToNextLevel, 0);
        MissionSystem missionSystem = new MissionSystem();
        missionSystem.Init(new MissionData());

        CoreLoopActionCoordinator coordinator = new CoreLoopActionCoordinator(
            petData,
            currencyData,
            progressionData,
            new RoomData(),
            new OnboardingData(),
            new PetSystem(petData),
            currencySystem,
            inventorySystem,
            shopSystem,
            progressionSystem,
            missionSystem,
            balanceConfig,
            2,
            new CoreLoopActionCoordinatorCallbacks());

        ShopPurchaseResult result = coordinator.TryBuyItem("food_basic", 10);

        Assert.False(result.success);
        Assert.AreEqual("Not enough coins", result.message);
        Assert.AreEqual(5, currencyData.coins);
        Assert.AreEqual(0, inventorySystem.GetItemCount("food_basic"));
    }

    [Test]
    public void OpenShop_UsesSeparatePanelAndReturnsHomeCleanly()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            AppShellUI shell = canvasObject.AddComponent<AppShellUI>();
            ShopPanelUI shopPanel = canvasObject.AddComponent<ShopPanelUI>();

            shell.SetDependencies(null, null, null, shopPanel, null, null, null, null);

            bool opened = shell.OpenShop();
            bool closed = shell.OpenHome();

            Assert.True(opened);
            Assert.True(closed);
            Assert.False(shopPanel.IsPanelVisible());
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void BuySkin_SpendsCoinsOnceAndMarksOwnership()
    {
        CurrencyData currencyData = new CurrencyData { coins = 200 };
        InventoryData inventoryData = new InventoryData();
        CurrencySystem currencySystem = new CurrencySystem(currencyData);
        InventorySystem inventorySystem = new InventorySystem(inventoryData);
        ShopSystem shopSystem = new ShopSystem(currencySystem, inventorySystem);

        bool firstPurchase = shopSystem.BuySkin("skin_midnight", 120);
        bool secondPurchase = shopSystem.BuySkin("skin_midnight", 120);

        Assert.True(firstPurchase);
        Assert.False(secondPurchase);
        Assert.AreEqual(80, currencyData.coins);
        Assert.True(inventorySystem.HasSkin("skin_midnight"));
    }

    [Test]
    public void ShopCatalog_ExposesCardsForAllConfiguredCategories()
    {
        BalanceConfig balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();

        ShopCatalogItemData[] catalog = ShopCatalogDefinitions.Create(balanceConfig);

        Assert.NotNull(catalog);
        Assert.GreaterOrEqual(catalog.Length, 12);
        Assert.True(System.Array.Exists(catalog, item => item.category == ShopCategory.Food));
        Assert.True(System.Array.Exists(catalog, item => item.category == ShopCategory.Energy));
        Assert.True(System.Array.Exists(catalog, item => item.category == ShopCategory.Mood));
        Assert.True(System.Array.Exists(catalog, item => item.category == ShopCategory.Skins));
        Assert.True(System.Array.Exists(catalog, item => item.category == ShopCategory.Special));
        Assert.True(System.Array.Exists(catalog, item => item.id == "care_treat"));
    }

    [Test]
    public void GetShopItems_CareCategoryShowsExplicitHungerAndMoodValues()
    {
        GameObject gameObject = new GameObject("GameManagerHost");
        try
        {
            GameManager manager = gameObject.AddComponent<GameManager>();
            manager.balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
            manager.progressionData = new ProgressionData { level = 1, xp = 0 };
            manager.currencyData = new CurrencyData { coins = 100 };
            manager.petData = new PetData { hunger = 50f, mood = 50f };
            manager.inventoryData = new InventoryData();
            manager.skillsData = new SkillsData();
            manager.roomData = new RoomData();
            manager.missionData = new MissionData();
            manager.dailyRewardData = new DailyRewardData();
            manager.onboardingData = new OnboardingData();
            manager.focusStateData = new FocusStateData();

            var initializeMethod = typeof(GameManager).GetMethod("InitializeRuntimeSystems", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(initializeMethod);
            initializeMethod.Invoke(manager, null);

            var items = manager.GetShopItems(ShopCategory.Energy);

            Assert.NotNull(items);
            Assert.IsTrue(items.Count >= 3);
            Assert.IsTrue(items.Exists(item => item.id == "care_treat" && item.description.Contains("+9 Hunger") && item.description.Contains("+9 Mood")));
            Assert.IsTrue(items.Exists(item => item.id == "energy_drink" && item.description.Contains("+15 Hunger") && item.description.Contains("+15 Mood")));
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    [Test]
    public void UseCareItem_ConsumesInventoryAndRestoresHungerAndMood()
    {
        BalanceConfig balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
        PetData petData = new PetData { hunger = 50f, mood = 40f, energy = 10f, hasIndependentStats = true };
        CurrencyData currencyData = new CurrencyData { coins = 0 };
        InventoryData inventoryData = new InventoryData();
        inventoryData.items.Add(new InventoryEntry { itemId = "energy_drink", count = 1 });
        ProgressionData progressionData = new ProgressionData { level = 2, xp = 0 };

        InventorySystem inventorySystem = new InventorySystem(inventoryData);
        CurrencySystem currencySystem = new CurrencySystem(currencyData);
        ShopSystem shopSystem = new ShopSystem(currencySystem, inventorySystem);
        ProgressionSystem progressionSystem = new ProgressionSystem(progressionData, balanceConfig.xpToNextLevel, 0);
        MissionSystem missionSystem = new MissionSystem();
        missionSystem.Init(new MissionData());
        PetSystem petSystem = new PetSystem(petData);

        CoreLoopActionCoordinator coordinator = new CoreLoopActionCoordinator(
            petData,
            currencyData,
            progressionData,
            new RoomData(),
            new OnboardingData(),
            petSystem,
            currencySystem,
            inventorySystem,
            shopSystem,
            progressionSystem,
            missionSystem,
            balanceConfig,
            2,
            new CoreLoopActionCoordinatorCallbacks());

        ShopPurchaseResult result = coordinator.TryUseConsumableItem("energy_drink", 30f, ShopCategory.Energy);

        Assert.True(result.success);
        Assert.AreEqual("Used Energy Drink", result.message);
        Assert.AreEqual(0, inventorySystem.GetItemCount("energy_drink"));
        Assert.AreEqual(65f, petData.hunger, 0.01f);
        Assert.AreEqual(55f, petData.mood, 0.01f);
    }

    [Test]
    public void EquipSkin_RequiresOwnershipAndUpdatesEquippedSkin()
    {
        InventoryData inventoryData = new InventoryData();
        InventorySystem inventorySystem = new InventorySystem(inventoryData);

        bool equipWithoutOwnership = inventorySystem.EquipSkin("skin_midnight");
        inventorySystem.AddSkin("skin_midnight");
        bool equipOwned = inventorySystem.EquipSkin("skin_midnight");

        Assert.False(equipWithoutOwnership);
        Assert.True(equipOwned);
        Assert.AreEqual("skin_midnight", inventorySystem.GetEquippedSkin());
    }
}
