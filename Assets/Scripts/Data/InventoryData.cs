using System.Collections.Generic;

[System.Serializable]
public class InventoryEntry
{
    public string itemId;
    public int count;
}

[System.Serializable]
public class InventoryData
{
    public List<InventoryEntry> items = new List<InventoryEntry>();

    // Legacy fields for backward compatibility
    public int food = 0;
    public int snack = 0;
    public int meal = 0;
    public int premium = 0;
}
