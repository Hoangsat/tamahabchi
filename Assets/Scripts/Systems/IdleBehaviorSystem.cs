using System;
using System.Collections.Generic;
using UnityEngine;

public interface IIdleRandomSource
{
    double NextDouble();
}

public sealed class DefaultIdleRandomSource : IIdleRandomSource
{
    private readonly System.Random random;

    public DefaultIdleRandomSource(int? seed = null)
    {
        random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
    }

    public double NextDouble()
    {
        return random.NextDouble();
    }
}

public readonly struct IdleRuntimeUpdate
{
    public IdleRuntimeUpdate(bool stateChanged, bool saveRequired, int newPendingEvents)
    {
        StateChanged = stateChanged;
        SaveRequired = saveRequired;
        NewPendingEvents = newPendingEvents;
    }

    public bool StateChanged { get; }
    public bool SaveRequired { get; }
    public int NewPendingEvents { get; }
}

internal sealed class IdleActionDefinition
{
    public IdleActionDefinition(string id, string archetypeId, string label, string tier, bool isBase)
    {
        Id = id ?? string.Empty;
        ArchetypeId = string.IsNullOrWhiteSpace(archetypeId) ? SkillArchetypeCatalog.General : SkillArchetypeCatalog.NormalizeArchetypeId(archetypeId);
        Label = label ?? string.Empty;
        Tier = tier ?? "simple";
        IsBase = isBase;
    }

    public string Id { get; }
    public string ArchetypeId { get; }
    public string Label { get; }
    public string Tier { get; }
    public bool IsBase { get; }
}

public sealed class IdleBehaviorSystem
{
    public const double ActionIntervalMinSeconds = 20d;
    public const double ActionIntervalMaxSeconds = 35d;
    public const double EventCooldownSeconds = 90d;
    public const int PendingEventCap = 10;
    public const double OfflineCapSeconds = 8d * 60d * 60d;
    public const double OfflineOpportunitySeconds = 15d * 60d;
    public const int OfflineEventCap = 4;
    public static Func<IIdleRandomSource> TestRandomSourceFactory;

    private static readonly IdleActionDefinition[] BaseActions =
    {
        new IdleActionDefinition("base_stand", SkillArchetypeCatalog.General, "стоит", "simple", true),
        new IdleActionDefinition("base_walk", SkillArchetypeCatalog.General, "ходит", "simple", true),
        new IdleActionDefinition("base_look", SkillArchetypeCatalog.General, "смотрит вокруг", "simple", true),
        new IdleActionDefinition("base_sit", SkillArchetypeCatalog.General, "сидит", "simple", true)
    };

    private static readonly Dictionary<string, IdleActionDefinition[]> ArchetypeActions =
        new Dictionary<string, IdleActionDefinition[]>(StringComparer.OrdinalIgnoreCase)
        {
            { SkillArchetypeCatalog.Logic, new[]
                {
                    new IdleActionDefinition("logic_think", SkillArchetypeCatalog.Logic, "думает", "simple", false),
                    new IdleActionDefinition("logic_solve", SkillArchetypeCatalog.Logic, "решает", "advanced", false)
                }
            },
            { SkillArchetypeCatalog.Learning, new[]
                {
                    new IdleActionDefinition("learning_read", SkillArchetypeCatalog.Learning, "читает", "simple", false),
                    new IdleActionDefinition("learning_study", SkillArchetypeCatalog.Learning, "изучает", "advanced", false)
                }
            },
            { SkillArchetypeCatalog.Languages, new[]
                {
                    new IdleActionDefinition("languages_repeat", SkillArchetypeCatalog.Languages, "повторяет", "simple", false),
                    new IdleActionDefinition("languages_speak", SkillArchetypeCatalog.Languages, "говорит", "advanced", false)
                }
            },
            { SkillArchetypeCatalog.Creativity, new[]
                {
                    new IdleActionDefinition("creativity_sketch", SkillArchetypeCatalog.Creativity, "делает набросок", "simple", false),
                    new IdleActionDefinition("creativity_draw", SkillArchetypeCatalog.Creativity, "рисует", "advanced", false)
                }
            },
            { SkillArchetypeCatalog.Fitness, new[]
                {
                    new IdleActionDefinition("fitness_warmup", SkillArchetypeCatalog.Fitness, "разминается", "simple", false),
                    new IdleActionDefinition("fitness_train", SkillArchetypeCatalog.Fitness, "тренируется", "advanced", false)
                }
            },
            { SkillArchetypeCatalog.Endurance, new[]
                {
                    new IdleActionDefinition("endurance_run", SkillArchetypeCatalog.Endurance, "бегает", "simple", false),
                    new IdleActionDefinition("endurance_pace", SkillArchetypeCatalog.Endurance, "держит темп", "advanced", false)
                }
            },
            { SkillArchetypeCatalog.Mindfulness, new[]
                {
                    new IdleActionDefinition("mindfulness_breathe", SkillArchetypeCatalog.Mindfulness, "дышит", "simple", false),
                    new IdleActionDefinition("mindfulness_meditate", SkillArchetypeCatalog.Mindfulness, "медитирует", "advanced", false)
                }
            },
            { SkillArchetypeCatalog.Productivity, new[]
                {
                    new IdleActionDefinition("productivity_plan", SkillArchetypeCatalog.Productivity, "планирует", "simple", false),
                    new IdleActionDefinition("productivity_checklist", SkillArchetypeCatalog.Productivity, "делает чеклист", "advanced", false)
                }
            },
            { SkillArchetypeCatalog.Craftsmanship, new[]
                {
                    new IdleActionDefinition("craftsmanship_tools", SkillArchetypeCatalog.Craftsmanship, "возится с инструментами", "simple", false),
                    new IdleActionDefinition("craftsmanship_craft", SkillArchetypeCatalog.Craftsmanship, "мастерит", "advanced", false)
                }
            },
            { SkillArchetypeCatalog.Culinary, new[]
                {
                    new IdleActionDefinition("culinary_snack", SkillArchetypeCatalog.Culinary, "готовит", "simple", false),
                    new IdleActionDefinition("culinary_cook", SkillArchetypeCatalog.Culinary, "колдует у плиты", "advanced", false)
                }
            },
            { SkillArchetypeCatalog.Music, new[]
                {
                    new IdleActionDefinition("music_play", SkillArchetypeCatalog.Music, "играет", "simple", false),
                    new IdleActionDefinition("music_rehearse", SkillArchetypeCatalog.Music, "репетирует", "advanced", false)
                }
            },
            { SkillArchetypeCatalog.Expression, new[]
                {
                    new IdleActionDefinition("expression_pose", SkillArchetypeCatalog.Expression, "репетирует", "simple", false),
                    new IdleActionDefinition("expression_perform", SkillArchetypeCatalog.Expression, "выступает", "advanced", false)
                }
            }
        };

    private static readonly HashSet<string> KnownItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "food_basic",
        "food_snack",
        "food_meal",
        "care_treat",
        "mood_toy",
        "food_premium"
    };

    private static readonly string[] ChestItemPool =
    {
        "food_basic",
        "food_snack",
        "food_meal",
        "care_treat",
        "mood_toy"
    };

    private static readonly string[] RareSkinPool =
    {
        "skin_midnight",
        "skin_sunrise"
    };

    private readonly IdleData idleData;
    private readonly IIdleRandomSource randomSource;

    public IdleBehaviorSystem(IdleData idleData, IIdleRandomSource randomSource = null)
    {
        this.idleData = idleData ?? new IdleData();
        this.randomSource = randomSource ?? CreateRandomSource();
        EnsureData();
    }

    public IdleData Data => idleData;

    public IdleRuntimeUpdate Tick(IReadOnlyList<SkillProgressionViewData> skillViews, bool rewardsBlocked, RoomData roomData, DateTime nowUtc)
    {
        EnsureData();

        bool stateChanged = EnsureCurrentAction(skillViews, nowUtc);
        int newPendingEvents = 0;

        if (nowUtc.Ticks >= idleData.nextActionAtUtcTicks)
        {
            IdleActionDefinition currentAction = SelectNextAction(skillViews);
            ApplyAction(currentAction, nowUtc);
            stateChanged = true;

            IdleEventEntryData createdEvent = TryCreateEvent(currentAction, skillViews, rewardsBlocked, roomData, nowUtc, "live");
            if (createdEvent != null)
            {
                idleData.pendingEvents.Add(createdEvent);
                idleData.lastEventAtUtcTicks = nowUtc.Ticks;
                stateChanged = true;
                newPendingEvents++;
            }
        }

        if (newPendingEvents > 0)
        {
            idleData.lastResolvedUtcTicks = nowUtc.Ticks;
        }

        return new IdleRuntimeUpdate(stateChanged, newPendingEvents > 0, newPendingEvents);
    }

    public IdleRuntimeUpdate ApplyOffline(double elapsedSeconds, IReadOnlyList<SkillProgressionViewData> skillViews, bool rewardsBlocked, RoomData roomData, DateTime nowUtc)
    {
        EnsureData();

        bool stateChanged = EnsureCurrentAction(skillViews, nowUtc);
        int newPendingEvents = 0;

        double sanitizedElapsed = TimeService.SanitizeElapsedSeconds(elapsedSeconds, OfflineCapSeconds);
        int opportunities = Mathf.Clamp(Mathf.FloorToInt((float)(sanitizedElapsed / OfflineOpportunitySeconds)), 0, OfflineEventCap);

        for (int i = 0; i < opportunities; i++)
        {
            if (idleData.pendingEvents.Count >= PendingEventCap || rewardsBlocked)
            {
                break;
            }

            IdleActionDefinition action = SelectNextAction(skillViews);
            DateTime opportunityTime = nowUtc.AddSeconds(-OfflineOpportunitySeconds * (opportunities - i));
            IdleEventEntryData createdEvent = TryCreateEvent(action, skillViews, rewardsBlocked, roomData, opportunityTime, "offline", ignoreCooldown: true);
            if (createdEvent == null)
            {
                continue;
            }

            idleData.pendingEvents.Add(createdEvent);
            idleData.lastEventAtUtcTicks = opportunityTime.Ticks;
            newPendingEvents++;
            stateChanged = true;
        }

        IdleActionDefinition latestAction = SelectNextAction(skillViews);
        ApplyAction(latestAction, nowUtc);
        idleData.lastResolvedUtcTicks = nowUtc.Ticks;
        stateChanged = true;

        return new IdleRuntimeUpdate(stateChanged, sanitizedElapsed > 0d || newPendingEvents > 0, newPendingEvents);
    }

    public string GetCurrentActionLabel()
    {
        EnsureData();
        IdleActionDefinition currentAction = GetActionById(idleData.currentActionId);
        return currentAction != null ? currentAction.Label : "отдыхает";
    }

    public string GetCurrentIconId()
    {
        EnsureData();
        return SkillArchetypeCatalog.GetCanonicalIcon(idleData.currentArchetypeId);
    }

    public string GetLatestSummary(bool rewardsBlocked)
    {
        EnsureData();

        if (idleData.pendingEvents.Count > 0)
        {
            IdleEventEntryData latestEvent = idleData.pendingEvents[idleData.pendingEvents.Count - 1];
            string summary = string.IsNullOrWhiteSpace(latestEvent.summary)
                ? latestEvent.title
                : latestEvent.summary;

            if (string.Equals(latestEvent.source, "offline", StringComparison.OrdinalIgnoreCase))
            {
                return "Пока вас не было: " + summary;
            }

            return summary;
        }

        if (rewardsBlocked)
        {
            return "Питомец отдыхает. Сначала верните hunger и mood.";
        }

        return "Пока ничего не нашёл.";
    }

    private static IIdleRandomSource CreateRandomSource()
    {
        if (TestRandomSourceFactory != null)
        {
            IIdleRandomSource overrideSource = TestRandomSourceFactory.Invoke();
            if (overrideSource != null)
            {
                return overrideSource;
            }
        }

        return new DefaultIdleRandomSource();
    }

    public static bool IsKnownItemId(string itemId)
    {
        return !string.IsNullOrWhiteSpace(itemId) && KnownItems.Contains(itemId.Trim());
    }

    public static bool IsKnownSkinId(string skinId)
    {
        if (string.IsNullOrWhiteSpace(skinId))
        {
            return false;
        }

        for (int i = 0; i < RareSkinPool.Length; i++)
        {
            if (string.Equals(RareSkinPool[i], skinId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetItemDisplayName(string itemId)
    {
        switch ((itemId ?? string.Empty).Trim())
        {
            case "food_basic":
                return "Basic Food";
            case "food_snack":
                return "Snack";
            case "food_meal":
                return "Meal";
            case "care_treat":
                return "Care Treat";
            case "mood_toy":
                return "Toy";
            case "food_premium":
                return "Premium Meal";
            default:
                return "Supplies";
        }
    }

    public static string GetSkinDisplayName(string skinId)
    {
        switch ((skinId ?? string.Empty).Trim())
        {
            case "skin_midnight":
                return "Midnight Skin";
            case "skin_sunrise":
                return "Sunrise Skin";
            default:
                return "Rare Skin";
        }
    }

    private void EnsureData()
    {
        idleData.pendingEvents ??= new List<IdleEventEntryData>();
        idleData.collectedMomentIds ??= new List<string>();
        idleData.currentArchetypeId = SkillArchetypeCatalog.NormalizeArchetypeId(idleData.currentArchetypeId);
    }

    private bool EnsureCurrentAction(IReadOnlyList<SkillProgressionViewData> skillViews, DateTime nowUtc)
    {
        if (!string.IsNullOrWhiteSpace(idleData.currentActionId) && idleData.nextActionAtUtcTicks > 0L)
        {
            return false;
        }

        ApplyAction(SelectNextAction(skillViews), nowUtc);
        idleData.lastResolvedUtcTicks = nowUtc.Ticks;
        return true;
    }

    private IdleActionDefinition SelectNextAction(IReadOnlyList<SkillProgressionViewData> skillViews)
    {
        List<SkillProgressionViewData> topSkills = GetTopSkills(skillViews);
        bool useBaseAction = topSkills.Count == 0 || randomSource.NextDouble() < 0.35d;
        if (useBaseAction)
        {
            return BaseActions[GetRandomIndex(BaseActions.Length)];
        }

        SkillProgressionViewData selectedSkill = SelectWeightedSkill(topSkills);
        bool useAdvanced = selectedSkill != null && selectedSkill.axisPercent >= 40f;
        return GetArchetypeAction(selectedSkill != null ? selectedSkill.archetypeId : SkillArchetypeCatalog.General, useAdvanced);
    }

    private void ApplyAction(IdleActionDefinition action, DateTime nowUtc)
    {
        IdleActionDefinition safeAction = action ?? BaseActions[0];
        idleData.currentActionId = safeAction.Id;
        idleData.currentArchetypeId = SkillArchetypeCatalog.NormalizeArchetypeId(safeAction.ArchetypeId);
        idleData.currentActionStartedAtUtcTicks = nowUtc.Ticks;
        idleData.nextActionAtUtcTicks = nowUtc.AddSeconds(GetRandomActionIntervalSeconds()).Ticks;
    }

    private IdleEventEntryData TryCreateEvent(
        IdleActionDefinition currentAction,
        IReadOnlyList<SkillProgressionViewData> skillViews,
        bool rewardsBlocked,
        RoomData roomData,
        DateTime nowUtc,
        string source,
        bool ignoreCooldown = false)
    {
        if (rewardsBlocked || idleData.pendingEvents.Count >= PendingEventCap)
        {
            return null;
        }

        if (!ignoreCooldown && idleData.lastEventAtUtcTicks > 0L)
        {
            double elapsedSinceLastEvent = TimeSpan.FromTicks(Math.Max(0L, nowUtc.Ticks - idleData.lastEventAtUtcTicks)).TotalSeconds;
            if (elapsedSinceLastEvent < EventCooldownSeconds)
            {
                return null;
            }
        }

        List<SkillProgressionViewData> topSkills = GetTopSkills(skillViews);
        SkillProgressionViewData strongestSkill = topSkills.Count > 0 ? topSkills[0] : null;
        float strongestAxis = strongestSkill != null ? Mathf.Clamp(strongestSkill.axisPercent, 0f, 100f) : 0f;
        double eventChance = 0.05d + (0.05d * (strongestAxis / 100f));
        if (randomSource.NextDouble() > eventChance)
        {
            return null;
        }

        IdleActionDefinition safeAction = currentAction ?? BaseActions[0];
        string eventArchetypeId = ResolveEventArchetypeId(safeAction, strongestSkill);
        string eventTier = strongestAxis >= 40f ? "advanced" : "simple";
        string eventType = RollEventType(strongestAxis);
        return BuildEvent(eventType, eventArchetypeId, safeAction, eventTier, nowUtc, source, roomData);
    }

    private IdleEventEntryData BuildEvent(
        string eventType,
        string archetypeId,
        IdleActionDefinition action,
        string tier,
        DateTime nowUtc,
        string source,
        RoomData roomData)
    {
        IdleEventEntryData entry = new IdleEventEntryData
        {
            id = Guid.NewGuid().ToString("N"),
            type = eventType ?? "coins",
            archetypeId = SkillArchetypeCatalog.NormalizeArchetypeId(archetypeId),
            createdAtUtcTicks = nowUtc.Ticks,
            source = string.IsNullOrWhiteSpace(source) ? "live" : source.Trim()
        };

        string archetypeName = SkillArchetypeCatalog.GetDisplayName(entry.archetypeId);
        string actionLabel = action != null ? action.Label : "гуляет";

        switch (entry.type)
        {
            case "chest":
                entry.coins = RandomRangeInclusive(10, 25);
                entry.itemId = ChestItemPool[GetRandomIndex(ChestItemPool.Length)];
                entry.title = "Нашёл сундук";
                entry.summary = $"Питомец нашёл {entry.coins} монет и {GetItemDisplayName(entry.itemId)}, пока {actionLabel}.";
                break;
            case "moment":
                entry.momentId = BuildMomentId(entry.archetypeId, action, tier);
                entry.title = "Особый момент";
                entry.summary = $"Запомнился особый момент: {archetypeName} / {actionLabel}.";
                break;
            case "rare":
                entry.skinId = RareSkinPool[GetRandomIndex(RareSkinPool.Length)];
                entry.title = "Редкая находка";
                entry.summary = $"Питомец принёс редкую находку во время занятия «{archetypeName}».";
                break;
            default:
                entry.type = "coins";
                entry.coins = RandomRangeInclusive(5, 15);
                entry.title = "Нашёл монеты";
                entry.summary = $"Питомец нашёл {entry.coins} монет, пока {actionLabel}.";
                break;
        }

        return entry;
    }

    private static string BuildMomentId(string archetypeId, IdleActionDefinition action, string tier)
    {
        string safeArchetype = SkillArchetypeCatalog.NormalizeArchetypeId(archetypeId);
        string safeActionId = action != null && !string.IsNullOrWhiteSpace(action.Id) ? action.Id : "idle";
        string safeTier = string.IsNullOrWhiteSpace(tier) ? "simple" : tier.Trim();
        return $"{safeArchetype}_{safeActionId}_{safeTier}";
    }

    private static string ResolveEventArchetypeId(IdleActionDefinition action, SkillProgressionViewData strongestSkill)
    {
        if (action != null && !action.IsBase && !string.Equals(action.ArchetypeId, SkillArchetypeCatalog.General, StringComparison.OrdinalIgnoreCase))
        {
            return action.ArchetypeId;
        }

        if (strongestSkill != null)
        {
            return strongestSkill.archetypeId;
        }

        return SkillArchetypeCatalog.General;
    }

    private string RollEventType(float strongestAxis)
    {
        int coinsWeight;
        int chestWeight;
        int momentWeight;
        int rareWeight;

        if (strongestAxis >= 75f)
        {
            coinsWeight = 60;
            chestWeight = 24;
            momentWeight = 11;
            rareWeight = 5;
        }
        else if (strongestAxis >= 40f)
        {
            coinsWeight = 68;
            chestWeight = 22;
            momentWeight = 8;
            rareWeight = 2;
        }
        else
        {
            coinsWeight = 75;
            chestWeight = 20;
            momentWeight = 5;
            rareWeight = 0;
        }

        int roll = RandomRangeInclusive(1, coinsWeight + chestWeight + momentWeight + rareWeight);
        if (roll <= coinsWeight)
        {
            return "coins";
        }

        roll -= coinsWeight;
        if (roll <= chestWeight)
        {
            return "chest";
        }

        roll -= chestWeight;
        if (roll <= momentWeight)
        {
            return "moment";
        }

        return "rare";
    }

    private SkillProgressionViewData SelectWeightedSkill(List<SkillProgressionViewData> topSkills)
    {
        if (topSkills == null || topSkills.Count == 0)
        {
            return null;
        }

        int[] baseWeights = { 50, 30, 20 };
        int totalWeight = 0;
        for (int i = 0; i < topSkills.Count && i < baseWeights.Length; i++)
        {
            totalWeight += baseWeights[i];
        }

        int roll = RandomRangeInclusive(1, Mathf.Max(1, totalWeight));
        for (int i = 0; i < topSkills.Count && i < baseWeights.Length; i++)
        {
            roll -= baseWeights[i];
            if (roll <= 0)
            {
                return topSkills[i];
            }
        }

        return topSkills[0];
    }

    private static List<SkillProgressionViewData> GetTopSkills(IReadOnlyList<SkillProgressionViewData> skillViews)
    {
        List<SkillProgressionViewData> ordered = new List<SkillProgressionViewData>();
        if (skillViews == null)
        {
            return ordered;
        }

        for (int i = 0; i < skillViews.Count; i++)
        {
            SkillProgressionViewData view = skillViews[i];
            if (view == null)
            {
                continue;
            }

            ordered.Add(view);
        }

        ordered.Sort((left, right) => right.axisPercent.CompareTo(left.axisPercent));
        if (ordered.Count > 3)
        {
            ordered.RemoveRange(3, ordered.Count - 3);
        }

        return ordered;
    }

    private IdleActionDefinition GetArchetypeAction(string archetypeId, bool useAdvanced)
    {
        string normalizedArchetypeId = SkillArchetypeCatalog.NormalizeArchetypeId(archetypeId);
        if (!ArchetypeActions.TryGetValue(normalizedArchetypeId, out IdleActionDefinition[] actionSet) || actionSet == null || actionSet.Length == 0)
        {
            return BaseActions[GetRandomIndex(BaseActions.Length)];
        }

        return useAdvanced && actionSet.Length > 1 ? actionSet[1] : actionSet[0];
    }

    private static IdleActionDefinition GetActionById(string actionId)
    {
        if (string.IsNullOrWhiteSpace(actionId))
        {
            return null;
        }

        for (int i = 0; i < BaseActions.Length; i++)
        {
            if (string.Equals(BaseActions[i].Id, actionId, StringComparison.OrdinalIgnoreCase))
            {
                return BaseActions[i];
            }
        }

        foreach (KeyValuePair<string, IdleActionDefinition[]> pair in ArchetypeActions)
        {
            IdleActionDefinition[] actionSet = pair.Value;
            for (int i = 0; i < actionSet.Length; i++)
            {
                if (string.Equals(actionSet[i].Id, actionId, StringComparison.OrdinalIgnoreCase))
                {
                    return actionSet[i];
                }
            }
        }

        return null;
    }

    private double GetRandomActionIntervalSeconds()
    {
        return ActionIntervalMinSeconds + (randomSource.NextDouble() * (ActionIntervalMaxSeconds - ActionIntervalMinSeconds));
    }

    private int GetRandomIndex(int length)
    {
        if (length <= 1)
        {
            return 0;
        }

        return Mathf.Clamp(Mathf.FloorToInt((float)(randomSource.NextDouble() * length)), 0, length - 1);
    }

    private int RandomRangeInclusive(int min, int max)
    {
        if (max <= min)
        {
            return min;
        }

        int span = (max - min) + 1;
        return min + GetRandomIndex(span);
    }
}
