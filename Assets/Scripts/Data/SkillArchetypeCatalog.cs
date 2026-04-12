using System;
using System.Collections.Generic;

public enum SkillArchetypeCategory
{
    Mental,
    Creative,
    Physical,
    Wellness,
    Practical,
    General
}

public sealed class SkillArchetypeDefinition
{
    public SkillArchetypeDefinition(
        string id,
        string displayName,
        SkillArchetypeCategory category,
        string canonicalIcon,
        string uiIconKey = "")
    {
        Id = id ?? string.Empty;
        DisplayName = displayName ?? string.Empty;
        Category = category;
        CanonicalIcon = canonicalIcon ?? string.Empty;
        UiIconKey = string.IsNullOrWhiteSpace(uiIconKey) ? CanonicalIcon : uiIconKey.Trim();
    }

    public string Id { get; }
    public string DisplayName { get; }
    public SkillArchetypeCategory Category { get; }
    public string CanonicalIcon { get; }
    public string UiIconKey { get; }
}

public static class SkillArchetypeCatalog
{
    public const string Logic = "logic";
    public const string Learning = "learning";
    public const string Languages = "languages";
    public const string Creativity = "creativity";
    public const string Fitness = "fitness";
    public const string Endurance = "endurance";
    public const string Mindfulness = "mindfulness";
    public const string Productivity = "productivity";
    public const string Craftsmanship = "craftsmanship";
    public const string Culinary = "culinary";
    public const string Music = "music";
    public const string Expression = "expression";
    public const string General = "general";

    private static readonly SkillArchetypeDefinition[] PlayerSelectableDefinitions =
    {
        new SkillArchetypeDefinition(Logic, "Логика и мышление", SkillArchetypeCategory.Mental, "MTH"),
        new SkillArchetypeDefinition(Learning, "Обучение и чтение", SkillArchetypeCategory.Mental, "BKS"),
        new SkillArchetypeDefinition(Languages, "Языки и коммуникация", SkillArchetypeCategory.Mental, "WRT"),
        new SkillArchetypeDefinition(Creativity, "Творчество и искусство", SkillArchetypeCategory.Creative, "ART"),
        new SkillArchetypeDefinition(Fitness, "Фитнес и сила", SkillArchetypeCategory.Physical, "SPT"),
        new SkillArchetypeDefinition(Endurance, "Выносливость и кардио", SkillArchetypeCategory.Physical, "END"),
        new SkillArchetypeDefinition(Mindfulness, "Осознанность и медитация", SkillArchetypeCategory.Wellness, "ZEN"),
        new SkillArchetypeDefinition(Productivity, "Продуктивность и планирование", SkillArchetypeCategory.Practical, "DEV"),
        new SkillArchetypeDefinition(Craftsmanship, "Ремёсла и рукоделие", SkillArchetypeCategory.Practical, "CRF"),
        new SkillArchetypeDefinition(Culinary, "Кулинария и дом", SkillArchetypeCategory.Practical, "CUL"),
        new SkillArchetypeDefinition(Music, "Музыка и ритм", SkillArchetypeCategory.Creative, "MSC"),
        new SkillArchetypeDefinition(Expression, "Самовыражение и сцена", SkillArchetypeCategory.Creative, "DNC")
    };

    private static readonly SkillArchetypeDefinition GeneralDefinition =
        new SkillArchetypeDefinition(General, "Общее", SkillArchetypeCategory.General, "SKL");

    private static readonly Dictionary<string, SkillArchetypeDefinition> DefinitionsById =
        new Dictionary<string, SkillArchetypeDefinition>(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> LegacyIconToArchetype =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "MTH", Logic },
            { "DEV", Logic },
            { "BKS", Learning },
            { "WRT", Languages },
            { "ART", Creativity },
            { "MSC", Music },
            { "DNC", Expression },
            { "SPT", Fitness },
            { "ZEN", Mindfulness },
            { "GME", General }
        };

    static SkillArchetypeCatalog()
    {
        for (int i = 0; i < PlayerSelectableDefinitions.Length; i++)
        {
            SkillArchetypeDefinition definition = PlayerSelectableDefinitions[i];
            DefinitionsById[definition.Id] = definition;
        }

        DefinitionsById[GeneralDefinition.Id] = GeneralDefinition;
    }

    public static IReadOnlyList<SkillArchetypeDefinition> GetPlayerSelectableDefinitions()
    {
        return PlayerSelectableDefinitions;
    }

    public static SkillArchetypeDefinition GetDefinition(string archetypeId)
    {
        string normalizedId = NormalizeArchetypeId(archetypeId);
        return DefinitionsById.TryGetValue(normalizedId, out SkillArchetypeDefinition definition)
            ? definition
            : GeneralDefinition;
    }

    public static bool IsSelectable(string archetypeId)
    {
        string normalizedId = NormalizeArchetypeId(archetypeId);
        return !string.Equals(normalizedId, General, StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeArchetypeId(string archetypeId)
    {
        string normalized = string.IsNullOrWhiteSpace(archetypeId)
            ? string.Empty
            : archetypeId.Trim().ToLowerInvariant();
        return DefinitionsById.ContainsKey(normalized) ? normalized : General;
    }

    public static string ResolveArchetypeIdFromLegacyIcon(string iconId)
    {
        string normalizedIcon = NormalizeIconToken(iconId);
        return LegacyIconToArchetype.TryGetValue(normalizedIcon, out string archetypeId)
            ? archetypeId
            : General;
    }

    public static string GetCanonicalIcon(string archetypeId)
    {
        return GetDefinition(archetypeId).CanonicalIcon;
    }

    public static string GetDisplayName(string archetypeId)
    {
        return GetDefinition(archetypeId).DisplayName;
    }

    public static string NormalizeIconForArchetype(string iconId, string archetypeId)
    {
        string normalizedArchetypeId = NormalizeArchetypeId(archetypeId);
        string normalizedIcon = NormalizeIconToken(iconId);
        SkillArchetypeDefinition definition = GetDefinition(normalizedArchetypeId);
        return string.Equals(normalizedIcon, definition.CanonicalIcon, StringComparison.OrdinalIgnoreCase)
            ? definition.CanonicalIcon
            : definition.CanonicalIcon;
    }

    public static string NormalizeIconToken(string iconId)
    {
        return string.IsNullOrWhiteSpace(iconId)
            ? string.Empty
            : iconId.Trim().ToUpperInvariant();
    }
}
