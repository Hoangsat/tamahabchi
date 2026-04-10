public class ShopSystem
{
    private CurrencySystem currencySystem;
    private InventorySystem inventorySystem;

    public ShopSystem(CurrencySystem currencySystem, InventorySystem inventorySystem)
    {
        this.currencySystem = currencySystem;
        this.inventorySystem = inventorySystem;
    }

    public bool BuyItem(string itemId, int price, int amount = 1)
    {
        if (amount <= 0 || string.IsNullOrEmpty(itemId)) return false;

        if (!currencySystem.SpendCoins(price))
            return false;

        inventorySystem.AddItem(itemId, amount);
        return true;
    }

    public bool CanAfford(int price)
    {
        return currencySystem != null && currencySystem.GetCoins() >= price;
    }

    public int GetOwnedCount(string itemId)
    {
        return inventorySystem != null ? inventorySystem.GetItemCount(itemId) : 0;
    }

    public bool BuySkin(string skinId, int price)
    {
        if (string.IsNullOrEmpty(skinId) || inventorySystem == null || currencySystem == null)
        {
            return false;
        }

        if (inventorySystem.HasSkin(skinId))
        {
            return false;
        }

        if (!currencySystem.SpendCoins(price))
        {
            return false;
        }

        inventorySystem.AddSkin(skinId);
        return true;
    }
}
