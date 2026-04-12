using System.Collections.Generic;

[System.Serializable]
public class IdleEventEntryData
{
    public string id = string.Empty;
    public string type = string.Empty;
    public string archetypeId = string.Empty;
    public string title = string.Empty;
    public string summary = string.Empty;
    public int coins = 0;
    public string itemId = string.Empty;
    public string skinId = string.Empty;
    public string momentId = string.Empty;
    public long createdAtUtcTicks = 0L;
    public string source = string.Empty;
}

[System.Serializable]
public class IdleData
{
    public string currentActionId = string.Empty;
    public string currentArchetypeId = string.Empty;
    public long currentActionStartedAtUtcTicks = 0L;
    public long nextActionAtUtcTicks = 0L;
    public long lastEventAtUtcTicks = 0L;
    public long lastResolvedUtcTicks = 0L;
    public List<IdleEventEntryData> pendingEvents = new List<IdleEventEntryData>();
    public List<string> collectedMomentIds = new List<string>();
}
