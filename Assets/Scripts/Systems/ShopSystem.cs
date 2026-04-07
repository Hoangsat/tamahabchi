public class ShopSystem
{
    private CurrencySystem currencySystem;
    private InventorySystem inventorySystem;

    public ShopSystem(CurrencySystem currencySystem, InventorySystem inventorySystem)
    {
        this.currencySystem = currencySystem;
        this.inventorySystem = inventorySystem;
    }

    public bool BuyFood(int price, int amount = 1)
    {
        if (amount <= 0) return false;

        if (!currencySystem.SpendCoins(price))
            return false;

        inventorySystem.AddFood(amount);
        return true;
    }
}
