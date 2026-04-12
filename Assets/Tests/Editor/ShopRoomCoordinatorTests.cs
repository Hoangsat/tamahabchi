using NUnit.Framework;
using UnityEngine;

public class ShopRoomCoordinatorTests
{
    [Test]
    public void GetRoomPanelState_ReportsCoinGateFromCurrentRoomLevel()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();
        balance.roomUpgrade1Cost = 25;
        balance.roomUpgradeMoodBonus = 8f;

        try
        {
            ShopRoomCoordinator coordinator = new ShopRoomCoordinator(
                balance,
                new RoomData { roomLevel = 0 },
                new CurrencyData { coins = 10 },
                null,
                null,
                null,
                null,
                2,
                new ShopRoomCoordinatorCallbacks());

            RoomPanelStateData state = coordinator.GetRoomPanelState();

            Assert.AreEqual(0, state.currentLevel);
            Assert.AreEqual(25, state.currentUpgradeCost);
            Assert.False(state.canUpgradeNow);
            Assert.AreEqual("Need 25 coins", state.blockedReason);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }

    [Test]
    public void TryEquipSkin_WhenOwnedUpdatesInventoryAndBroadcasts()
    {
        BalanceConfig balance = ScriptableObject.CreateInstance<BalanceConfig>();

        try
        {
            CurrencyData currencyData = new CurrencyData { coins = 150 };
            InventoryData inventoryData = new InventoryData();
            CurrencySystem currencySystem = new CurrencySystem(currencyData);
            InventorySystem inventorySystem = new InventorySystem(inventoryData);
            ShopSystem shopSystem = new ShopSystem(currencySystem, inventorySystem);
            inventorySystem.AddSkin("skin_midnight");

            bool inventoryChanged = false;
            bool petChanged = false;
            bool saved = false;
            bool uiUpdated = false;
            string feedback = string.Empty;

            ShopRoomCoordinator coordinator = new ShopRoomCoordinator(
                balance,
                new RoomData(),
                currencyData,
                currencySystem,
                inventorySystem,
                shopSystem,
                null,
                2,
                new ShopRoomCoordinatorCallbacks
                {
                    OnInventoryChanged = () => inventoryChanged = true,
                    BroadcastPetChanged = () => petChanged = true,
                    SaveGame = () => saved = true,
                    UpdateUi = () => uiUpdated = true,
                    ShowFeedback = message => feedback = message
                });

            bool success = coordinator.TryEquipSkin("skin_midnight", out string message);

            Assert.True(success);
            Assert.AreEqual("Equipped Midnight Skin", message);
            Assert.AreEqual("skin_midnight", inventorySystem.GetEquippedSkin());
            Assert.True(inventoryChanged);
            Assert.True(petChanged);
            Assert.True(saved);
            Assert.True(uiUpdated);
            Assert.AreEqual(message, feedback);
        }
        finally
        {
            Object.DestroyImmediate(balance);
        }
    }
}
