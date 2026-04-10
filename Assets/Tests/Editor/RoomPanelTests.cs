using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class RoomPanelTests
{
    [Test]
    public void OpenRoom_UsesSeparatePanelAndReturnsHomeCleanly()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        try
        {
            AppShellUI shell = canvasObject.AddComponent<AppShellUI>();
            RoomPanelUI roomPanel = canvasObject.AddComponent<RoomPanelUI>();

            shell.SetDependencies(null, null, null, null, roomPanel, null, null);

            bool opened = shell.OpenRoom();
            bool closed = shell.OpenHome();

            Assert.True(opened);
            Assert.True(closed);
            Assert.False(roomPanel.IsPanelVisible());
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void GetRoomPanelState_ReturnsCorrectLevelCostAndMaxState()
    {
        BalanceConfig balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
        balanceConfig.roomUpgrade1Cost = 25;
        balanceConfig.roomUpgrade2Cost = 50;
        balanceConfig.roomUpgrade1UnlockLevel = 2;
        balanceConfig.roomUpgrade2UnlockLevel = 4;
        balanceConfig.roomUpgradeMoodBonus = 8f;

        GameObject managerObject = new GameObject("GameManager");
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();
            manager.balanceConfig = balanceConfig;
            manager.petData = new PetData { hunger = 50f, mood = 50f, energy = 50f, hasIndependentStats = true };
            manager.currencyData = new CurrencyData { coins = 20 };
            manager.progressionData = new ProgressionData { level = 1, xp = 0 };
            manager.roomData = new RoomData { roomLevel = 0 };

            RoomPanelStateData levelZero = manager.GetRoomPanelState();
            Assert.AreEqual(0, levelZero.currentLevel);
            Assert.AreEqual(25, levelZero.currentUpgradeCost);
            Assert.AreEqual(2, levelZero.currentUnlockLevel);
            Assert.AreEqual("Unlock at level 2", levelZero.blockedReason);
            Assert.False(levelZero.isMaxLevel);

            manager.progressionData.level = 4;
            manager.currencyData.coins = 50;
            manager.roomData.roomLevel = 1;

            RoomPanelStateData levelOne = manager.GetRoomPanelState();
            Assert.AreEqual(1, levelOne.currentLevel);
            Assert.AreEqual(50, levelOne.currentUpgradeCost);
            Assert.AreEqual(4, levelOne.currentUnlockLevel);
            Assert.True(levelOne.canUpgradeNow);
            Assert.AreEqual("Dream Room", levelOne.nextVisualStateLabel);

            manager.roomData.roomLevel = 2;

            RoomPanelStateData levelTwo = manager.GetRoomPanelState();
            Assert.True(levelTwo.isMaxLevel);
            Assert.AreEqual("Room is max level", levelTwo.blockedReason);
            Assert.AreEqual(0, levelTwo.currentUpgradeCost);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(balanceConfig);
        }
    }

    [Test]
    public void GetRoomPanelState_ReportsBlockedReasonsForDeadPetAndLowCoins()
    {
        BalanceConfig balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
        balanceConfig.roomUpgrade1Cost = 25;
        balanceConfig.roomUpgrade1UnlockLevel = 2;

        GameObject managerObject = new GameObject("GameManager");
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();
            manager.balanceConfig = balanceConfig;
            manager.petData = new PetData { isDead = true, hasIndependentStats = true };
            manager.currencyData = new CurrencyData { coins = 100 };
            manager.progressionData = new ProgressionData { level = 5, xp = 0 };
            manager.roomData = new RoomData { roomLevel = 0 };

            RoomPanelStateData deadState = manager.GetRoomPanelState();
            Assert.AreEqual("Pet is dead", deadState.blockedReason);

            manager.petData.isDead = false;
            manager.currencyData.coins = 5;

            RoomPanelStateData lowCoinsState = manager.GetRoomPanelState();
            Assert.AreEqual("Need 25 coins", lowCoinsState.blockedReason);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(balanceConfig);
        }
    }

    [Test]
    public void TryUpgradeRoomFromPanel_ChangesRoomLevelAndCoins()
    {
        BalanceConfig balanceConfig = ScriptableObject.CreateInstance<BalanceConfig>();
        balanceConfig.roomUpgrade1Cost = 25;
        balanceConfig.roomUpgrade1UnlockLevel = 2;
        balanceConfig.roomUpgradeMoodBonus = 8f;

        PetData petData = new PetData { hunger = 50f, mood = 10f, energy = 50f, hasIndependentStats = true };
        CurrencyData currencyData = new CurrencyData { coins = 40 };
        InventoryData inventoryData = new InventoryData();
        ProgressionData progressionData = new ProgressionData { level = 2, xp = 0 };
        RoomData roomData = new RoomData { roomLevel = 0 };
        OnboardingData onboardingData = new OnboardingData();

        CurrencySystem currencySystem = new CurrencySystem(currencyData);
        InventorySystem inventorySystem = new InventorySystem(inventoryData);
        ShopSystem shopSystem = new ShopSystem(currencySystem, inventorySystem);
        ProgressionSystem progressionSystem = new ProgressionSystem(progressionData, balanceConfig.xpToNextLevel, balanceConfig.buyUnlockLevel);
        MissionSystem missionSystem = new MissionSystem();
        missionSystem.Init(new MissionData());

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
            new CoreLoopActionCoordinatorCallbacks());

        GameObject managerObject = new GameObject("GameManager");
        try
        {
            GameManager manager = managerObject.AddComponent<GameManager>();
            manager.balanceConfig = balanceConfig;
            manager.petData = petData;
            manager.currencyData = currencyData;
            manager.inventoryData = inventoryData;
            manager.progressionData = progressionData;
            manager.roomData = roomData;

            FieldInfo coordinatorField = typeof(GameManager).GetField("coreLoopActionCoordinator", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(coordinatorField);
            coordinatorField.SetValue(manager, coordinator);

            bool success = manager.TryUpgradeRoomFromPanel(out string message);

            Assert.True(success);
            Assert.AreEqual("Room upgraded to level 1", message);
            Assert.AreEqual(1, roomData.roomLevel);
            Assert.AreEqual(15, currencyData.coins);
            Assert.AreEqual(18f, petData.mood, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(balanceConfig);
        }
    }
}
