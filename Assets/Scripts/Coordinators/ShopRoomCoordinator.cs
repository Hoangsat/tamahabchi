using System;
using System.Collections.Generic;

public sealed class ShopRoomCoordinatorCallbacks
{
    public Action OnInventoryChanged;
    public Action BroadcastPetChanged;
    public Action SaveGame;
    public Action UpdateUi;
    public Action<string> ShowFeedback;
}

public sealed class ShopRoomCoordinator
{
    private readonly BalanceConfig balanceConfig;
    private readonly RoomData roomData;
    private readonly CurrencyData currencyData;
    private readonly CurrencySystem currencySystem;
    private readonly InventorySystem inventorySystem;
    private readonly ShopSystem shopSystem;
    private readonly CoreLoopActionCoordinator coreLoopActionCoordinator;
    private readonly int maxSupportedRoomLevel;
    private readonly ShopRoomCoordinatorCallbacks callbacks;

    public ShopRoomCoordinator(
        BalanceConfig balanceConfig,
        RoomData roomData,
        CurrencyData currencyData,
        CurrencySystem currencySystem,
        InventorySystem inventorySystem,
        ShopSystem shopSystem,
        CoreLoopActionCoordinator coreLoopActionCoordinator,
        int maxSupportedRoomLevel,
        ShopRoomCoordinatorCallbacks callbacks)
    {
        this.balanceConfig = balanceConfig;
        this.roomData = roomData;
        this.currencyData = currencyData;
        this.currencySystem = currencySystem;
        this.inventorySystem = inventorySystem;
        this.shopSystem = shopSystem;
        this.coreLoopActionCoordinator = coreLoopActionCoordinator;
        this.maxSupportedRoomLevel = maxSupportedRoomLevel;
        this.callbacks = callbacks ?? new ShopRoomCoordinatorCallbacks();
    }

    public RoomPanelStateData GetRoomPanelState()
    {
        int currentLevel = roomData != null ? roomData.roomLevel : 0;
        return RoomPanelStatePresenter.Build(balanceConfig, currentLevel, maxSupportedRoomLevel, GetCurrentCoins(), 0);
    }

    public bool TryUpgradeRoom(out string message)
    {
        if (coreLoopActionCoordinator == null)
        {
            message = "Room unavailable";
            return false;
        }

        ShopPurchaseResult result = coreLoopActionCoordinator.TryUpgradeRoom();
        message = result.message;
        return result.success;
    }

    public List<ShopItemViewData> GetShopItems(ShopCategory category)
    {
        List<ShopItemViewData> items = new List<ShopItemViewData>();
        ShopCatalogItemData[] catalog = ShopCatalogDefinitions.Create(balanceConfig);
        if (catalog == null)
        {
            return items;
        }

        int currentCoins = GetCurrentCoins();
        string equippedSkinId = GetEquippedSkinId();

        for (int i = 0; i < catalog.Length; i++)
        {
            ShopCatalogItemData definition = catalog[i];
            if (definition != null && definition.category == category)
            {
                items.Add(ShopPanelPresenter.BuildItemView(definition, currentCoins, GetOwnedShopItemCount(definition), equippedSkinId));
            }
        }

        return items;
    }

    public string GetShopPlaceholderMessage(ShopCategory category)
    {
        return ShopPanelPresenter.GetPlaceholderMessage(category);
    }

    public string GetShopCategoryStatus(ShopCategory category)
    {
        return ShopPanelPresenter.GetCategoryStatus(category);
    }

    public bool TryPurchaseItem(string itemId, out string message)
    {
        ShopCatalogItemData definition = GetShopCatalogItem(itemId);
        if (definition == null)
        {
            message = "Item unavailable";
            return false;
        }

        if (!definition.purchaseEnabled)
        {
            message = string.IsNullOrEmpty(definition.unavailableReason) ? "Coming soon" : definition.unavailableReason;
            return false;
        }

        ShopPurchaseResult result = definition.kind == ShopItemKind.Cosmetic
            ? TryBuySkin(definition.id, definition.price)
            : TryBuyItem(definition.id, definition.price);
        message = result.message;
        return result.success;
    }

    public bool TryUseItem(string itemId, out string message)
    {
        ShopCatalogItemData definition = GetShopCatalogItem(itemId);
        if (definition == null)
        {
            message = "Item unavailable";
            return false;
        }

        if (definition.kind != ShopItemKind.Consumable)
        {
            message = "Item cannot be used";
            return false;
        }

        ShopPurchaseResult result = coreLoopActionCoordinator != null
            ? coreLoopActionCoordinator.TryUseConsumableItem(definition.id, definition.effectValue, definition.category)
            : ShopPurchaseResult.Fail("Shop unavailable");
        message = result.message;
        return result.success;
    }

    public bool TryEquipSkin(string itemId, out string message)
    {
        ShopCatalogItemData definition = GetShopCatalogItem(itemId);
        if (definition == null || definition.kind != ShopItemKind.Cosmetic)
        {
            message = "Skin unavailable";
            return false;
        }

        if (inventorySystem == null || !inventorySystem.HasSkin(itemId))
        {
            message = "Buy first";
            return false;
        }

        if (string.Equals(GetEquippedSkinId(), itemId, StringComparison.Ordinal))
        {
            message = "Already equipped";
            return false;
        }

        bool equipped = inventorySystem.EquipSkin(itemId);
        if (!equipped)
        {
            message = "Equip failed";
            return false;
        }

        callbacks.OnInventoryChanged?.Invoke();
        callbacks.BroadcastPetChanged?.Invoke();
        callbacks.UpdateUi?.Invoke();
        callbacks.SaveGame?.Invoke();
        message = $"Equipped {definition.displayName}";
        callbacks.ShowFeedback?.Invoke(message);
        return true;
    }

    public string GetEquippedSkinId()
    {
        return inventorySystem != null ? inventorySystem.GetEquippedSkin() : "default";
    }

    private int GetCurrentCoins()
    {
        if (currencySystem != null)
        {
            return currencySystem.GetCoins();
        }

        return currencyData != null ? currencyData.coins : 0;
    }

    private ShopPurchaseResult TryBuyItem(string itemId, int price)
    {
        return coreLoopActionCoordinator != null
            ? coreLoopActionCoordinator.TryBuyItem(itemId, price)
            : ShopPurchaseResult.Fail("Shop unavailable");
    }

    private ShopPurchaseResult TryBuySkin(string itemId, int price)
    {
        return coreLoopActionCoordinator != null
            ? coreLoopActionCoordinator.TryBuySkin(itemId, price)
            : ShopPurchaseResult.Fail("Shop unavailable");
    }

    private int GetOwnedShopItemCount(ShopCatalogItemData definition)
    {
        if (definition == null)
        {
            return 0;
        }

        if (definition.kind == ShopItemKind.Cosmetic)
        {
            return inventorySystem != null && inventorySystem.HasSkin(definition.id) ? 1 : 0;
        }

        return shopSystem != null ? shopSystem.GetOwnedCount(definition.id) : (inventorySystem != null ? inventorySystem.GetItemCount(definition.id) : 0);
    }

    private ShopCatalogItemData GetShopCatalogItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        ShopCatalogItemData[] catalog = ShopCatalogDefinitions.Create(balanceConfig);
        if (catalog == null)
        {
            return null;
        }

        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] != null && string.Equals(catalog[i].id, itemId, StringComparison.Ordinal))
            {
                return catalog[i];
            }
        }

        return null;
    }
}
