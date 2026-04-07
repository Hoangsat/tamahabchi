public class InventorySystem
{
    private InventoryData inventoryData;

    public InventorySystem(InventoryData inventoryData)
    {
        this.inventoryData = inventoryData;
    }

    public int GetFood()
    {
        return inventoryData.food;
    }

    public void AddFood(int amount)
    {
        if (amount <= 0) return;
        inventoryData.food += amount;
    }

    public bool HasFood(int amount = 1)
    {
        return inventoryData.food >= amount;
    }

    public bool ConsumeFood(int amount = 1)
    {
        if (amount <= 0) return false;
        if (inventoryData.food < amount) return false;

        inventoryData.food -= amount;
        return true;
    }
}
