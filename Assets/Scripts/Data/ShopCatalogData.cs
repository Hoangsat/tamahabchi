using System;
using UnityEngine;

public enum ShopCategory
{
    Food,
    Energy,
    Mood,
    Skins,
    Special
}

public enum ShopItemKind
{
    Consumable,
    Cosmetic,
    Special
}

[Serializable]
public class ShopCatalogItemData
{
    public string id = string.Empty;
    public string displayName = string.Empty;
    public string description = string.Empty;
    public string iconLabel = string.Empty;
    public int price;
    public int effectValue;
    public bool purchaseEnabled = true;
    public string unavailableReason = string.Empty;
    public ShopCategory category = ShopCategory.Food;
    public ShopItemKind kind = ShopItemKind.Consumable;
}

[Serializable]
public class ShopItemViewData
{
    public string id = string.Empty;
    public string displayName = string.Empty;
    public string description = string.Empty;
    public string iconLabel = string.Empty;
    public int price;
    public int ownedCount;
    public bool canBuy;
    public string disabledReason = string.Empty;
    public ShopCategory category = ShopCategory.Food;
    public ShopItemKind kind = ShopItemKind.Consumable;
    public bool showUseAction;
    public bool canUseAction;
    public string useActionLabel = string.Empty;
}

public static class ShopCatalogDefinitions
{
    public static ShopCatalogItemData[] Create(BalanceConfig balanceConfig)
    {
        return new[]
        {
            new ShopCatalogItemData
            {
                id = "food_basic",
                displayName = "Basic Food",
                description = $"Restore {GetRoundedValue(balanceConfig != null ? balanceConfig.feedAmount : 10f)} hunger.",
                iconLabel = "BASIC",
                price = balanceConfig != null ? balanceConfig.foodPrice : 10,
                effectValue = Mathf.RoundToInt(balanceConfig != null ? balanceConfig.feedAmount : 10f),
                category = ShopCategory.Food,
                kind = ShopItemKind.Consumable
            },
            new ShopCatalogItemData
            {
                id = "food_snack",
                displayName = "Snack",
                description = $"Restore {GetRoundedValue(balanceConfig != null ? balanceConfig.snackRestore : 15f)} hunger.",
                iconLabel = "SNACK",
                price = balanceConfig != null ? balanceConfig.snackPrice : 5,
                effectValue = Mathf.RoundToInt(balanceConfig != null ? balanceConfig.snackRestore : 15f),
                category = ShopCategory.Food,
                kind = ShopItemKind.Consumable
            },
            new ShopCatalogItemData
            {
                id = "food_meal",
                displayName = "Meal",
                description = $"Restore {GetRoundedValue(balanceConfig != null ? balanceConfig.mealRestore : 40f)} hunger.",
                iconLabel = "MEAL",
                price = balanceConfig != null ? balanceConfig.mealPrice : 20,
                effectValue = Mathf.RoundToInt(balanceConfig != null ? balanceConfig.mealRestore : 40f),
                category = ShopCategory.Food,
                kind = ShopItemKind.Consumable
            },
            new ShopCatalogItemData
            {
                id = "food_premium",
                displayName = "Premium Meal",
                description = $"Restore {GetRoundedValue(balanceConfig != null ? balanceConfig.premiumRestore : 80f)} hunger.",
                iconLabel = "PREM",
                price = balanceConfig != null ? balanceConfig.premiumPrice : 50,
                effectValue = Mathf.RoundToInt(balanceConfig != null ? balanceConfig.premiumRestore : 80f),
                category = ShopCategory.Food,
                kind = ShopItemKind.Consumable
            },
            new ShopCatalogItemData
            {
                id = "energy_drink",
                displayName = "Care Drink",
                description = "Restore hunger and mood together.",
                iconLabel = "NRGY",
                price = 40,
                effectValue = 30,
                category = ShopCategory.Energy,
                kind = ShopItemKind.Consumable
            },
            new ShopCatalogItemData
            {
                id = "care_treat",
                displayName = "Care Treat",
                description = "Restore a small amount of hunger and mood together.",
                iconLabel = "TREAT",
                price = 20,
                effectValue = 18,
                category = ShopCategory.Energy,
                kind = ShopItemKind.Consumable
            },
            new ShopCatalogItemData
            {
                id = "energy_boost",
                displayName = "Care Boost",
                description = "Restore a bigger amount of hunger and mood together.",
                iconLabel = "BOOST",
                price = 65,
                effectValue = 50,
                category = ShopCategory.Energy,
                kind = ShopItemKind.Consumable
            },
            new ShopCatalogItemData
            {
                id = "mood_toy",
                displayName = "Toy",
                description = "Raise mood by 25.",
                iconLabel = "TOY",
                price = 30,
                effectValue = 25,
                category = ShopCategory.Mood,
                kind = ShopItemKind.Consumable
            },
            new ShopCatalogItemData
            {
                id = "mood_music_box",
                displayName = "Music Box",
                description = "Raise mood by 35.",
                iconLabel = "MUSIC",
                price = 45,
                effectValue = 35,
                category = ShopCategory.Mood,
                kind = ShopItemKind.Consumable
            },
            new ShopCatalogItemData
            {
                id = "skin_midnight",
                displayName = "Midnight Skin",
                description = "Unlock a cool midnight style for your pet.",
                iconLabel = "NIGHT",
                price = 120,
                category = ShopCategory.Skins,
                kind = ShopItemKind.Cosmetic
            },
            new ShopCatalogItemData
            {
                id = "skin_sunrise",
                displayName = "Sunrise Skin",
                description = "Unlock a bright sunrise style for your pet.",
                iconLabel = "SUN",
                price = 150,
                category = ShopCategory.Skins,
                kind = ShopItemKind.Cosmetic
            },
            new ShopCatalogItemData
            {
                id = "special_care_kit",
                displayName = "Care Kit",
                description = "Expanded care systems are planned for a later build.",
                iconLabel = "CARE",
                price = 50,
                purchaseEnabled = false,
                unavailableReason = "Coming soon",
                category = ShopCategory.Special,
                kind = ShopItemKind.Special
            },
            new ShopCatalogItemData
            {
                id = "special_skill_slot_chip",
                displayName = "Skill Slot Chip",
                description = "Extra skill slots are planned for a later build.",
                iconLabel = "SLOT",
                price = 90,
                purchaseEnabled = false,
                unavailableReason = "Coming soon",
                category = ShopCategory.Special,
                kind = ShopItemKind.Special
            }
        };
    }

    private static string GetRoundedValue(float value)
    {
        return Mathf.RoundToInt(value).ToString();
    }
}

public struct ShopPurchaseResult
{
    public bool success;
    public string message;

    public static ShopPurchaseResult Success(string message)
    {
        return new ShopPurchaseResult
        {
            success = true,
            message = message ?? string.Empty
        };
    }

    public static ShopPurchaseResult Fail(string message)
    {
        return new ShopPurchaseResult
        {
            success = false,
            message = message ?? string.Empty
        };
    }
}
