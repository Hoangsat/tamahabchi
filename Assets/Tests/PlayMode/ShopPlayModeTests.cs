using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ShopPlayModeTests
{
    private const string MainSceneName = "Main";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        new SaveManager().Reset();
        yield return SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        new SaveManager().Reset();
        yield return null;
    }

    [UnityTest]
    public IEnumerator MainScene_ShopFlow_BuyUseEquipAndPersistAfterSceneReload_Works()
    {
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        AppShellUI shell = Object.FindAnyObjectByType<AppShellUI>();
        ShopPanelUI shopPanel = Object.FindAnyObjectByType<ShopPanelUI>();

        Assert.NotNull(manager);
        Assert.NotNull(shell);
        Assert.NotNull(shopPanel);

        manager.currencyData.coins = 300;
        manager.petData.hunger = 30f;
        yield return null;

        Assert.True(shell.OpenShop());
        yield return null;

        Assert.True(shopPanel.IsPanelVisible());
        Assert.False(manager.feedButton != null && manager.feedButton.gameObject.activeSelf);

        int coinsBeforeSnack = manager.GetCurrentCoins();
        float hungerBeforeSnack = manager.petData.hunger;

        bool boughtSnack = manager.TryPurchaseShopItem("food_snack", out string buySnackMessage);
        Assert.True(boughtSnack, buySnackMessage);
        Assert.AreEqual(1, GetItemCount(manager.inventoryData, "food_snack"));
        Assert.Less(manager.GetCurrentCoins(), coinsBeforeSnack);

        bool usedSnack = manager.TryUseShopItem("food_snack", out string useSnackMessage);
        Assert.True(usedSnack, useSnackMessage);
        Assert.AreEqual(0, GetItemCount(manager.inventoryData, "food_snack"));
        Assert.Greater(manager.petData.hunger, hungerBeforeSnack);

        bool boughtSkin = manager.TryPurchaseShopItem("skin_midnight", out string buySkinMessage);
        Assert.True(boughtSkin, buySkinMessage);
        Assert.True(HasSkin(manager.inventoryData, "skin_midnight"));

        bool equippedSkin = manager.TryEquipShopSkin("skin_midnight", out string equipMessage);
        Assert.True(equippedSkin, equipMessage);
        Assert.AreEqual("skin_midnight", manager.GetEquippedSkinId());

        manager.SaveGame();

        yield return SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
        yield return null;

        GameManager reloadedManager = Object.FindAnyObjectByType<GameManager>();
        ShopPanelUI reloadedShopPanel = Object.FindAnyObjectByType<ShopPanelUI>();

        Assert.NotNull(reloadedManager);
        Assert.NotNull(reloadedShopPanel);
        Assert.AreEqual("skin_midnight", reloadedManager.GetEquippedSkinId());
        Assert.True(HasSkin(reloadedManager.inventoryData, "skin_midnight"));

        Assert.True(reloadedManager.OpenShopPanel());
        yield return null;

        Assert.True(reloadedShopPanel.IsPanelVisible());
    }

    private static int GetItemCount(InventoryData inventoryData, string itemId)
    {
        if (inventoryData == null || inventoryData.items == null)
        {
            return 0;
        }

        InventoryEntry entry = inventoryData.items.Find(item => item != null && item.itemId == itemId);
        return entry != null ? entry.count : 0;
    }

    private static bool HasSkin(InventoryData inventoryData, string skinId)
    {
        return inventoryData != null
            && inventoryData.ownedSkins != null
            && inventoryData.ownedSkins.Contains(skinId);
    }
}
