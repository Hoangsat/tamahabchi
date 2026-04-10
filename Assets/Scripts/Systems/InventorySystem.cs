using System.Collections.Generic;

public class InventorySystem
{
    private InventoryData inventoryData;

    public InventorySystem(InventoryData inventoryData)
    {
        this.inventoryData = inventoryData;
    }

    private InventoryEntry GetEntry(string itemId)
    {
        if (inventoryData.items == null)
            inventoryData.items = new List<InventoryEntry>();
            
        return inventoryData.items.Find(e => e.itemId == itemId);
    }

    public int GetItemCount(string itemId)
    {
        var entry = GetEntry(itemId);
        return entry != null ? entry.count : 0;
    }

    public void AddItem(string itemId, int amount = 1)
    {
        if (amount <= 0) return;
        var entry = GetEntry(itemId);
        if (entry != null)
        {
            entry.count += amount;
        }
        else
        {
            inventoryData.items.Add(new InventoryEntry { itemId = itemId, count = amount });
        }
    }

    public bool HasItem(string itemId, int amount = 1)
    {
        return GetItemCount(itemId) >= amount;
    }

    public bool ConsumeItem(string itemId, int amount = 1)
    {
        if (amount <= 0) return false;
        var entry = GetEntry(itemId);
        if (entry == null || entry.count < amount) return false;

        entry.count -= amount;
        return true;
    }

    public bool HasSkin(string skinId)
    {
        if (inventoryData == null || inventoryData.ownedSkins == null || string.IsNullOrEmpty(skinId))
        {
            return false;
        }

        return inventoryData.ownedSkins.Contains(skinId);
    }

    public void AddSkin(string skinId)
    {
        if (inventoryData == null || string.IsNullOrEmpty(skinId))
        {
            return;
        }

        inventoryData.ownedSkins ??= new List<string>();
        if (!inventoryData.ownedSkins.Contains(skinId))
        {
            inventoryData.ownedSkins.Add(skinId);
        }
    }

    public string GetEquippedSkin()
    {
        if (inventoryData == null || string.IsNullOrEmpty(inventoryData.equippedSkin))
        {
            return "default";
        }

        return inventoryData.equippedSkin;
    }

    public bool EquipSkin(string skinId)
    {
        if (inventoryData == null || string.IsNullOrEmpty(skinId))
        {
            return false;
        }

        if (!string.Equals(skinId, "default") && !HasSkin(skinId))
        {
            return false;
        }

        inventoryData.equippedSkin = skinId;
        return true;
    }
}
