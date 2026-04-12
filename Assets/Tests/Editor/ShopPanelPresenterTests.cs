using NUnit.Framework;

public class ShopPanelPresenterTests
{
    [Test]
    public void BuildItemView_EnergyConsumableFormatsSplitRestoreAndUseAction()
    {
        ShopCatalogItemData definition = new ShopCatalogItemData
        {
            id = "care_treat",
            displayName = "Care Treat",
            description = "Restore both stats.",
            iconLabel = "CARE",
            price = 20,
            effectValue = 18,
            category = ShopCategory.Energy,
            kind = ShopItemKind.Consumable
        };

        ShopItemViewData view = ShopPanelPresenter.BuildItemView(definition, 100, 2, string.Empty);

        Assert.AreEqual("+9 Hunger  |  +9 Mood", view.description);
        Assert.True(view.canBuy);
        Assert.True(view.showUseAction);
        Assert.True(view.canUseAction);
        Assert.AreEqual("Care", view.useActionLabel);
    }

    [Test]
    public void BuildItemView_CosmeticOwnedAndEquipped_DisablesBuyAndShowsEquipped()
    {
        ShopCatalogItemData definition = new ShopCatalogItemData
        {
            id = "skin_midnight",
            displayName = "Midnight Skin",
            description = "Unlock a skin.",
            iconLabel = "NIGHT",
            price = 120,
            category = ShopCategory.Skins,
            kind = ShopItemKind.Cosmetic
        };

        ShopItemViewData view = ShopPanelPresenter.BuildItemView(definition, 400, 1, "skin_midnight");

        Assert.False(view.canBuy);
        Assert.AreEqual("Owned", view.disabledReason);
        Assert.True(view.showUseAction);
        Assert.False(view.canUseAction);
        Assert.AreEqual("Equipped", view.useActionLabel);
    }

    [Test]
    public void CategoryCopy_StaysConsistentForSpecialTab()
    {
        Assert.AreEqual("Special items are coming soon.", ShopPanelPresenter.GetPlaceholderMessage(ShopCategory.Special));
        Assert.AreEqual("Special items are visible, but still reserved for future systems.", ShopPanelPresenter.GetCategoryStatus(ShopCategory.Special));
    }

    [Test]
    public void BuildCategorySummary_SkinsShowsEquippedSkinAndUnlockedCount()
    {
        var items = new System.Collections.Generic.List<ShopItemViewData>
        {
            new ShopItemViewData { id = "skin_midnight", kind = ShopItemKind.Cosmetic, ownedCount = 1 },
            new ShopItemViewData { id = "skin_sunrise", kind = ShopItemKind.Cosmetic, ownedCount = 1 },
            new ShopItemViewData { id = "skin_forest", kind = ShopItemKind.Cosmetic, ownedCount = 0 }
        };

        string summary = ShopPanelPresenter.BuildCategorySummary(ShopCategory.Skins, items, "skin_midnight");

        Assert.AreEqual("Equipped skin: Midnight  |  Unlocked: 2", summary);
    }

    [Test]
    public void BuildStatusText_JoinsPetSummaryAndMessage()
    {
        string status = ShopPanelPresenter.BuildStatusText("Hunger 70  |  Mood 60", "Food is fully active in this build.", "Bought Basic");

        Assert.AreEqual("Hunger 70  |  Mood 60\nBought Basic", status);
    }
}
