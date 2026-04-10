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
}
