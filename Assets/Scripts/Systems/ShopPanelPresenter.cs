using System;
using UnityEngine;

public static class ShopPanelPresenter
{
    public static ShopItemViewData BuildItemView(ShopCatalogItemData definition, int currentCoins, int ownedCount, string equippedSkinId)
    {
        if (definition == null)
        {
            return new ShopItemViewData();
        }

        bool alreadyOwned = definition.kind == ShopItemKind.Cosmetic && ownedCount > 0;
        bool isEquipped = definition.kind == ShopItemKind.Cosmetic && string.Equals(equippedSkinId, definition.id, StringComparison.Ordinal);
        bool canBuy = definition.purchaseEnabled && !alreadyOwned && currentCoins >= definition.price;
        string disabledReason = GetDisabledReason(definition, currentCoins, alreadyOwned);

        bool showUseAction = false;
        bool canUseAction = false;
        string useActionLabel = string.Empty;

        if (definition.kind == ShopItemKind.Consumable)
        {
            showUseAction = true;
            if (ownedCount <= 0)
            {
                useActionLabel = definition.category == ShopCategory.Food ? "No stock" : "No item";
            }
            else
            {
                useActionLabel = definition.category == ShopCategory.Food
                    ? "Feed"
                    : definition.category == ShopCategory.Energy
                        ? "Care"
                        : "Use";
                canUseAction = true;
            }
        }
        else if (definition.kind == ShopItemKind.Cosmetic)
        {
            showUseAction = true;
            if (!alreadyOwned)
            {
                useActionLabel = "Buy first";
            }
            else if (isEquipped)
            {
                useActionLabel = "Equipped";
            }
            else
            {
                useActionLabel = "Equip";
                canUseAction = true;
            }
        }

        return new ShopItemViewData
        {
            id = definition.id,
            displayName = definition.displayName,
            description = BuildItemDescription(definition),
            iconLabel = definition.iconLabel,
            price = definition.price,
            ownedCount = ownedCount,
            canBuy = canBuy,
            disabledReason = disabledReason,
            category = definition.category,
            kind = definition.kind,
            showUseAction = showUseAction,
            canUseAction = canUseAction,
            useActionLabel = useActionLabel
        };
    }

    public static string GetPlaceholderMessage(ShopCategory category)
    {
        switch (category)
        {
            case ShopCategory.Energy:
                return "Care items help restore hunger and mood together.";
            case ShopCategory.Mood:
                return "Mood boosts are coming soon.";
            case ShopCategory.Skins:
                return "Skins are coming soon.";
            case ShopCategory.Special:
                return "Special items are coming soon.";
            default:
                return "Food is fully active in this build.";
        }
    }

    public static string GetCategoryStatus(ShopCategory category)
    {
        switch (category)
        {
            case ShopCategory.Food:
                return "Buy food here, then use Feed on the item card.";
            case ShopCategory.Energy:
                return "Care items restore both hunger and mood. Buy them, then press Care on the item card.";
            case ShopCategory.Mood:
                return "Mood items can be bought here, then used from their item card.";
            case ShopCategory.Skins:
                return "Skins are one-time unlocks and can be equipped here.";
            case ShopCategory.Special:
                return "Special items are visible, but still reserved for future systems.";
            default:
                return string.Empty;
        }
    }

    public static string BuildScreenTitle(ShopCategory category)
    {
        return $"Shop / {GetCategoryLabel(category)}";
    }

    public static string BuildCoinsLabel(int coins)
    {
        return $"Coins: {coins}";
    }

    public static string BuildStatusText(string petSummary, string categoryStatus, string messageOverride = null)
    {
        string categoryText = string.IsNullOrEmpty(messageOverride) ? categoryStatus : messageOverride;
        if (string.IsNullOrEmpty(petSummary))
        {
            return categoryText;
        }

        return $"{petSummary}\n{categoryText}";
    }

    public static string BuildCategorySummary(ShopCategory category, System.Collections.Generic.List<ShopItemViewData> items, string equippedSkinId)
    {
        if (category == ShopCategory.Skins)
        {
            int unlockedSkins = 0;
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] != null && items[i].kind == ShopItemKind.Cosmetic && items[i].ownedCount > 0)
                    {
                        unlockedSkins++;
                    }
                }
            }

            return $"Equipped skin: {FormatSkinLabel(equippedSkinId)}  |  Unlocked: {unlockedSkins}";
        }

        if (category == ShopCategory.Special)
        {
            return "Special shelf: preview-only in this build.";
        }

        int totalOwned = 0;
        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null)
                {
                    totalOwned += Mathf.Max(0, items[i].ownedCount);
                }
            }
        }

        string categoryLabel = GetCategoryLabel(category);
        return totalOwned > 0
            ? $"{categoryLabel} stock: {totalOwned}"
            : $"{categoryLabel} stock: empty";
    }

    public static string BuildPriceLabel(ShopItemViewData item)
    {
        if (item == null)
        {
            return "Price: --";
        }

        return item.kind == ShopItemKind.Cosmetic
            ? $"Unlock: {item.price}"
            : $"Price: {item.price}";
    }

    public static string BuildOwnedLabel(ShopItemViewData item)
    {
        if (item == null)
        {
            return "Owned: --";
        }

        if (item.kind == ShopItemKind.Cosmetic)
        {
            return item.ownedCount > 0 ? "Owned: unlocked" : "Owned: not yet";
        }

        return $"Owned: {item.ownedCount}";
    }

    public static string GetTabLabel(ShopCategory category)
    {
        return category == ShopCategory.Energy ? "Care" : GetCategoryLabel(category);
    }

    public static string GetCategoryLabel(ShopCategory category)
    {
        switch (category)
        {
            case ShopCategory.Energy:
                return "Care";
            case ShopCategory.Mood:
                return "Mood";
            case ShopCategory.Skins:
                return "Skins";
            case ShopCategory.Special:
                return "Special";
            default:
                return "Food";
        }
    }

    private static string GetDisabledReason(ShopCatalogItemData definition, int currentCoins, bool alreadyOwned)
    {
        if (!definition.purchaseEnabled)
        {
            return string.IsNullOrEmpty(definition.unavailableReason) ? "Coming soon" : definition.unavailableReason;
        }

        if (alreadyOwned)
        {
            return "Owned";
        }

        if (currentCoins < definition.price)
        {
            return $"Need {definition.price - currentCoins} coins";
        }

        return string.Empty;
    }

    private static string BuildItemDescription(ShopCatalogItemData definition)
    {
        switch (definition.category)
        {
            case ShopCategory.Food:
                return $"+{definition.effectValue} Hunger";
            case ShopCategory.Energy:
                int hungerGain = Mathf.FloorToInt(definition.effectValue * 0.5f);
                int moodGain = definition.effectValue - hungerGain;
                return $"+{hungerGain} Hunger  |  +{moodGain} Mood";
            case ShopCategory.Mood:
                return $"+{definition.effectValue} Mood";
            default:
                return definition.description;
        }
    }

    private static string FormatSkinLabel(string skinId)
    {
        if (string.IsNullOrEmpty(skinId) || string.Equals(skinId, "default"))
        {
            return "Default";
        }

        string trimmed = skinId.StartsWith("skin_") ? skinId.Substring(5) : skinId;
        string[] parts = trimmed.Split('_');
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrEmpty(parts[i]))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(char.ToUpper(parts[i][0]));
            if (parts[i].Length > 1)
            {
                builder.Append(parts[i].Substring(1));
            }
        }

        return builder.Length > 0 ? builder.ToString() : "Default";
    }
}
