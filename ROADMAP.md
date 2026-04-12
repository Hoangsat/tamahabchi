

# TAMAHABCHI — ROADMAP 

## 📌 Навигация по документу
1. [Product North Star & Vision](#1-product-north-star--vision)
2. [Архитектура проекта (Anti‑Cargo Cult)](#2-архитектура-проекта)
3. [Система навыков (ядро геймплея)](#3-система-навыков-ядро-геймплея)
4. [Паутинная диаграмма (Radar Chart) — ТОП-12](#4-паутинная-диаграмма-radar-chart--топ-12)
5. [Все UI экраны + флоу](#5-все-ui-экраны--флоу)
6. [Экономика и баланс (математика)](#6-экономика-и-баланс-математика)
7. [Система питомца (Tamagotchi Core)](#7-система-питомца-tamagotchi-core)
8. [Система заданий — ДВА ТИПА (навыковые + рутины)](#8-система-заданий--два-типа-навыковые--рутины)
9. [Режим фокуса (таймер + награды)](#9-режим-фокуса-таймер--награды)
10. [Магазин, инвентарь, валюта](#10-магазин-инвентарь-валюта)
11. [Боевая система — УМНЫЕ БОССЫ (сравнение паутин)](#11-боевая-система--умные-боссы-сравнение-паутин)
12. [Комната питомца (кастомизация)](#12-комната-питомца-кастомизация)
13. [Прогрессия и достижения](#13-прогрессия-и-достижения)
14. [Звук, музыка, анимации](#14-звук-музыка-анимации)
15. [Сохранения и оффлайн‑режим](#15-сохранения-и-оффлайнрежим)
16. [Спринты разработки (11 спринтов)](#16-спринты-разработки-11-спринтов)
17. [Критерии приёмки (расширенные)](#17-критерии-приёмки-расширенные)
18. [Что НЕ делаем в v1 (но запишем в v2)](#18-что-не-делаем-в-v1-но-запишем-в-v2)
19. [Риски и компромиссы](#19-риски-и-компромиссы)
20. [Монетизация (опционально для будущего)](#20-монетизация-опционально-для-будущего)

---

## 1. Product North Star & Vision

> **Миссия:** Помочь игроку улучшать реальные навыки и формировать полезные привычки через игру, где забота о питомце, паутинная диаграмма и умные боссы создают позитивное подкрепление.

- **Ключевое обещание:**  
  «Ты развиваешь навыки и следишь за рутинами в реальной жизни — твой питомец, паутина и боссы растут вместе с тобой. Нет ограничений на количество навыков — хоть 100».
- **Уникальные фишки:**
  - Паутинная диаграмма ТОП-12 (видно самые сильные стороны)
  - Два типа заданий: навыковые (фокус) + рутины (вода, сон, душ)
  - Умные боссы, которые показывают твои слабые места через сравнение паутин
- **Эмоция:** Тёплая забота + гордость от заполненной паутины + лёгкий вызов (боссы подсказывают, что развивать).
- **Сессионность:** 5–15 минут в день активного ухода + сессии фокуса от 15 до 60 минут.

### 1.1 Актуальный статус реализации (на 2026-04-13)

| Блок | Статус | Комментарий |
|------|--------|-------------|
| Skills + Radar | **Implemented** | Навыки без жёсткого лимита, truth-model = `totalSP`, уровни `Lv.0..Lv.10`, radar по `axisPercent` через level bands, свой `RadarChartGraphic`; поверх навыков уже добавлен semantic-layer `Skill Archetype` с archetype picker и save migration |
| MissionSystem core | **Implemented** | Daily reset 05:00, weighted generation, claim flow, product-layer UI (`Skill Missions` + `Routines`), custom create flow, 5/5 bonus |
| Mission debug telemetry | **Implemented** | Dev-only snapshot/report для generation pipeline |
| Focus core | **Implemented** | Полный `FocusPanel` UX собран: выбор навыка и длительности, pause/resume, cancel, finish early, result screen, save/restore after restart |
| Idle System | **Implemented (v1)** | Status-first idle слой уже в runtime: действия питомца по `Skill Archetype`, Home inbox с `Забрать`, modest rewards (`coins/chest/moment/rare`), capped offline-pass и без выдачи `SP` |
| Pet core | **Partial** | Базовые статы и core flow стабильны; `Critical / Neglected` уже используются как gate для idle rewards, но полноценный pet/room product-layer polish ещё впереди |
| Shop / Inventory | **Implemented** | Отдельный `ShopPanel`, категории, purchase/use/equip flow, inventory integration |
| Room screen | **Implemented** | Отдельный `RoomPanel`, upgrade-first vertical slice, save/load и shell integration |
| Battle | **Implemented (v1)** | `BattlePanel` и `BattleSystem` уже в проекте: roster боссов, `Player Battle Power` vs `Boss Power`, energy gate, reward preview и result UX; core acceptance пройден, дальше — visual/mobile polish |
| Architecture cleanup | **Mostly complete** | `GameManager` уже разгружен, heavy UI cleanup по `Shop/Skills/HUD/Focus/Room/Mission` выполнен, pre-idle полный `EditMode` regression-pass = `175/175 passed`; поверх этой базы уже встроен `Idle v1`, а текущие остаточные hotspots = `MissionSystem.Generation.cs`, mixed scene/runtime UI assembly, visual/mobile polish и playmode-strengthening |
| Save / Load | **Implemented** | File-based JSON save в `Application.persistentDataPath`, backup/temp files, normalizer/migration уже подняты до `saveVersion = 7` и теперь включают `Skill Archetype` + `IdleData` |

### 1.1.1 Product Lock: pet XP / pet level removed from current scope

На `2026-04-12` фиксируем для roadmap:

- `pet XP` и `pet level` не участвуют в активном product loop;
- `Focus`, `Missions`, `Routines`, `Battle`, `Shop` и `Feed` не выдают опыт питомцу;
- `Room` upgrade и прочие продуктовые unlock-и не используют уровень питомца;
- основной прогресс игрока идёт через `coins`, `pet state` и рост навыков через `SP`;
- поля `progression.level` / `progression.xp` могут оставаться в save data только как legacy-совместимость;
- legacy `rewardXp` в mission/battle runtime и старые XP-gain knobs в `BalanceConfig` больше не являются активным контрактом и удалены из текущего runtime-слоя.

Если ниже в roadmap ещё встречаются упоминания `pet XP` или `pet level`, считать их историческими заметками, а не текущим product contract.

### 1.1.2 Актуальный следующий этап

На `2026-04-13` ближайшее движение по проекту фиксируем так:

- не открываем новый большой gameplay-system поверх текущего vertical slice;
- `Phase 1` и `Phase 2` acceptance уже подтверждены: shell navigation, save/load, pause/resume и offline apply ведут себя стабильно;
- `Phase 3` и `Phase 4` не выявили blocker-ов по product-логике; heavy UI cleanup по `ShopPanelUI -> SkillsPanelUI -> HUDUI -> FocusPanelUI -> RoomPanelUI -> MissionPanelUI` уже выполнен;
- последний подтверждённый полный edit-mode regression-pass на cleanup-этапе = `175/175 passed`; после интеграции `Idle v1` опираемся на clean compile, targeted editor/playmode tests и runtime smoke, потому что сводный `Unity-MCP` totals в этой среде шумный;
- skill-layer уже усилен `Skill Archetype` слоем: archetype picker в `Skills`, смена типа существующего навыка, compatibility wrapper для старого `icon`-пути и migration старых save;
- `Idle v1` уже интегрирован: status-first Home idle block, pending inbox, capped live/offline rewards и claim-flow без пассивного `SP`;
- следующий инженерный спринт = targeted visual/mobile polish для `Home` idle block и result/popup states в `Focus`, `Missions` и `Battle`, content/icon art pass для archetype-иконок и усиление runtime/playmode regression, а не новый system-layer rewrite;
- playmode/runtime regression дальше усиливаем сценариями вокруг `Idle`, `Focus restore`, `Shop`, `Missions`, `Battle` и `Room`, а не открываем ещё один большой core-system;
- `MissionSystem.Generation.cs` дальше дробим только если это реально нужно под новые mission-фичи;
- `GameManager` больше не считать единственным blocker-ом: он остаётся composition root, а главный остаточный инженерный риск теперь живёт в `MissionPanelUI`, `MissionSystem.Generation.cs` и mixed scene/runtime UI assembly;
- старые sprint-чеклисты ниже считать историей исполнения и сверять с актуальной статус-таблицей выше.

Практический execution-чеклист для этого этапа вынесен в [ACCEPTANCE_CHECKLIST.md](/G:/Tamahabchi/ACCEPTANCE_CHECKLIST.md).

Техническая оговорка: `Unity-MCP` в этой среде ненадёжен для `execute_code` в `play mode`, поэтому runtime acceptance сейчас опирается на сочетание editor-tests, edit-mode проверок и ручного screen-pass.

### 1.2 Актуальная skill progression model (override для старых percent-секций)

Ниже по документу ещё могут встречаться legacy-упоминания `percent` как основной метрики навыка. Для текущего кода это уже **неактуально**. Реальная модель проекта на `2026-04-11` такая:

- Truth-model навыка: `totalSP`
- Вычисляемые значения: `level`, `progressInLevel`, `requiredSPForNextLevel`, `progressInLevel01`, `axisPercent`
- Уровни: `Lv.0..Lv.10`
- Таблица переходов: `100, 160, 256, 410, 656, 1050, 1680, 2688, 4300, 6880`
- Паутина (`Radar`) считается **не** как `totalSP / maxSP`, а через level bands:
  - каждый завершённый уровень = `10%` оси
  - текущий прогресс уровня заполняет следующий `10%` сегмент
  - maxed skill = `100%` ось
- Новый навык стартует как:
  - `Level 0`
  - `totalSP = 0`
  - `axisPercent = 0`
- `Golden / Maxed` теперь привязаны к `Level 10`, а не к старому `percent >= 100`
- `Focus` награждает навыки через `SP`, а не через `%`
- `Mission` skill rewards тоже идут через `SP`
- Старое поле `percent` оставлено только как legacy migration field для старых сейвов
- Поверх свободного имени навыка теперь живёт semantic-layer `archetypeId`
- Поле `icon` сохранено как compatibility/fallback visual token (`MTH`, `ART`, `MSC` и т.д.), а не как player-facing смысловой тип
- Старые сейвы без `archetypeId` поднимаются через `SaveNormalizer`: `icon -> archetypeId`, неизвестные значения уходят в `general`

Практическое правило для чтения roadmap ниже:

- где написано `процент навыка` как source of truth, читать это как legacy-дизайн;
- где описаны текущие runtime loops (`Skills`, `Focus`, `Save/Load`, `Radar`), приоритет имеет именно модель `SP -> Level -> Axis Percent`.

---

### 1.3 ?????????? pet model (override ??? ?????? death/revive-??????)
???? ?? roadmap ??? ????? ??????????? legacy-????? ??? death / revive, energy ??? core-???? ? revive-?????????. ??? ???????? ???? ??? ??? **???????????**.
???????? runtime-?????? ??????? ?? 2026-04-11:
- ???????? punishment-????????: PetState.Neglected
- ???? ? Neglected:
  - hunger <= 0 && mood <= 0
- ????? ?? Neglected:
  - hunger > 0 || mood > 0
- ?? ????? Neglected:
  - Focus ??????????
  - skill decay ???? ????? decayDebtSP
  - rate ? 1: +1 decayDebtSP ?? ?????? ?????? ??? neglect
- ???? ?????? ????????? ???:
  - effectiveSP = max(0, totalSP - decayDebtSP)
- Radar, ranking ? battle power ?????????? effectiveSP
- Level ?????? ????????? ?????? ?? 	otalSP ? ?? ?????? ??-?? neglect
- Focus ??????????????? ???? ?????? ????? ???? 	otalSP:
  - 1 ?????? = 1 SP
  - -1 mood ?? ??????
- Energy ?????? ?? ????????? ? core loop:
  - ???????? ?????? ??? legacy save-field
  - ?? ???????????? ??? ??????????, focus cost/reward ? ????????? UX
- Dead / Revive flow ?????? ?? ???????? ???????? ??????????? ?????????
???????????? ??????? ?????? roadmap ????:
- ??? ??????? ?????? ???????, revive token, revive economy ??? energy ??? runtime-core, ?????? ??? ??? legacy-??????;
- ??? ??????? ??????? pet/skills/battle loops, ????????? ????? ?????? ?????? Neglect + decayDebtSP + effectiveSP.

## 2. Архитектура проекта

### 2.1 Слои (строгие правила — не нарушать)

| Слой | Компоненты | Что нельзя делать |
|------|------------|------------------|
| **Bootstrap** | GameBootstrap, SceneLoader | Любую UI‑логику |
| **Core** | GameManager (только связывает) | Хранить данные игрока, считать баланс |
| **Systems** | PetSystem, SkillSystem, MissionSystem, BattleSystem, ShopSystem | Обращаться к Transform/Button, знать о UI |
| **Data** | PlayerData, SaveManager, Definitions | Содержать игровую логику |
| **UI** | Все Panel (HomePanel, SkillPanel, MissionPanel, FocusPanel, ShopPanel, BattlePanel, RoomPanel), Button, Text | Считать награды, менять save напрямую |
| **Events** | EventBus (C# events / UnityEvents) | Прямые ссылки между системами |

**Текущая runtime-реальность:**
- `Systems` уже являются source of truth для миссий, навыков, питомца и магазина.
- `GameManager` остаётся composition root и facade для UI, но главный остаточный риск уже сместился в `MissionPanelUI`, `MissionSystem.Generation.cs` и mixed scene/runtime UI assembly, а не в сам `GameManager`.
- `Idle v1` уже встроен в runtime через `IdleBehaviorSystem + IdleCoordinator + HUDUI`, а не как отдельный временный мод.
- `Bootstrap`, `SceneLoader` и полноценный `EventBus` остаются целевым направлением, а не текущим фактом.
- Схема папок ниже описывает target structure, а не точную карту текущего репозитория один-в-один.

### 2.2 Папки в Unity

```
Assets/
├── _Project/
│   ├── Scenes/
│   │   ├── Bootstrap.unity
│   │   └── Main.unity
│   ├── Scripts/
│   │   ├── Bootstrap/
│   │   │   ├── GameBootstrap.cs
│   │   │   └── SceneLoader.cs
│   │   ├── Core/
│   │   │   ├── GameManager.cs
│   │   │   └── EventBus.cs
│   │   ├── Systems/
│   │   │   ├── PetSystem.cs
│   │   │   ├── SkillSystem.cs
│   │   │   ├── MissionSystem.cs
│   │   │   ├── BattleSystem.cs
│   │   │   ├── ShopSystem.cs
│   │   │   └── ProgressionSystem.cs
│   │   ├── Data/
│   │   │   ├── PlayerData.cs
│   │   │   ├── SaveManager.cs
│   │   │   └── Definitions/
│   │   │       ├── SkillDefinition.cs
│   │   │       ├── ItemDefinition.cs
│   │   │       ├── BossDefinition.cs
│   │   │       └── BalanceConfig.cs
│   │   ├── UI/
│   │   │   ├── Panels/
│   │   │   │   ├── HomePanel.cs
│   │   │   │   ├── SkillPanel.cs
│   │   │   │   ├── MissionPanel.cs
│   │   │   │   ├── FocusPanel.cs
│   │   │   │   ├── ShopPanel.cs
│   │   │   │   ├── BattlePanel.cs
│   │   │   │   └── RoomPanel.cs
│   │   │   ├── Widgets/
│   │   │   │   ├── SkillListItem.cs
│   │   │   │   ├── MissionListItem.cs
│   │   │   │   ├── RoutineListItem.cs
│   │   │   │   └── RadarChartGraphic.cs
│   │   │   └── UIAnimations.cs
│   │   └── Utils/
│   │       ├── TimeHelper.cs
│   │       └── MathHelper.cs
│   ├── Prefabs/
│   │   ├── UI/
│   │   ├── Pet/
│   │   └── Items/
│   ├── Art/
│   │   ├── Sprites/
│   │   │   ├── Pet/
│   │   │   ├── Icons/
│   │   │   └── UI/
│   │   └── Animations/
│   ├── Audio/
│   │   ├── SFX/
│   │   └── Music/
│   └── Configs/
│       ├── BalanceConfig.asset
│       └── SkillIconsConfig.asset
```

### 2.3 EventBus (примеры событий)

```csharp
public class SkillProgressEvent {
    public string skillId;
    public string skillName;
    public float oldPercent;
    public float newPercent;
    public float deltaPercent;
}

public class FocusCompletedEvent {
    public string skillId;
    public int minutes;
    public float skillGain;
    public int coinsReward;
    public int xpReward;
}

public class PetStatChangedEvent {
    public PetStatType stat; // Hunger, Energy, Mood, Health
    public int oldValue;
    public int newValue;
}

public class RoutineCompletedEvent {
    public string routineId;
    public string routineName;
    public int coinsReward;
    public int xpReward;
}
```

---

## 3. Система навыков (ядро геймплея)

### 3.1 Skill Model (расширенный)

```csharp
[Serializable]
public class Skill {
    public string id;                 // UUID, генерируется при создании
    public string name;               // "Математика", макс 20 символов
    public string icon;               // canonical fallback token: "MTH", "ART", "MSC"
    public string archetypeId;        // "logic", "music", "mindfulness" и т.д.
    public int totalSP;               // source of truth для прогрессии
}
```

### 3.2 Правила (железобетонные)

| № | Правило |
|---|---------|
| 1 | **НЕТ ОГРАНИЧЕНИЯ** на количество навыков. Можно добавить 100+. Проверено на 500+ |
| 2 | Удалить навык можно ТОЛЬКО если `totalSP == 0` |
| 3 | Если `totalSP > 0` → кнопка 🗑️ НЕАКТИВНА (disabled) |
| 4 | Навык растёт ТОЛЬКО от фокуса на этот конкретный навык; рост считается в `SP` |
| 5 | При достижении 100%: питомец получает перманентный бонус +5% к опыту за фокус на этот навык |
| 6 | При 100% вершина на паутине становится ЗОЛОТОЙ и пульсирует |
| 7 | Первые 3 навыка — бесплатно. 4-й и далее — 50 монет за новый слот |

### 3.3 Формула роста навыка

```csharp
int CalculateSkillPointsFromFocusDuration(float durationSeconds) {
    if (durationSeconds <= 0f) {
        return 0;
    }

    return Mathf.Max(0, Mathf.FloorToInt(durationSeconds / 60f));
}
```

**Примеры:**
- 15 минут → `15 SP`
- 25 минут → `25 SP`
- 60 минут → `60 SP`
- дальнейший UI-прогресс считается через `SP -> Level -> axisPercent`

### 3.4 SkillSystem API

```csharp
public class SkillSystem {
    public List<Skill> GetSkills();
    public List<SkillProgressionViewData> GetSkillProgressionViews();
    public Skill AddSkillWithArchetype(string name, string archetypeId);
    public Skill AddSkill(string name, string icon); // legacy compatibility wrapper
    public bool ChangeSkillArchetype(string skillId, string archetypeId);
    public bool RemoveSkill(string skillId);    // только если totalSP == 0
    public SkillProgressResult ApplySkillPoints(string skillId, int deltaSP, ...);
}
```

---

## 4. Паутинная диаграмма (Radar Chart) — ТОП-12

### 4.1 Логика отображения

| Количество навыков | Что показывает диаграмма |
|-------------------|--------------------------|
| 0–2 | Сообщение: «Добавьте хотя бы 3 навыка» + кнопка «➕ Добавить навык» |
| 3–12 | Все навыки (3–12 осей) |
| 13+ | ТОП-12 по проценту (при равных процентах — по дате последнего фокуса) |
| >20 | ТОП-12 + кнопка «След. страница» (опционально) |

### 4.2 Математика отрисовки

```
Угол между осями = 360° / количество_отображаемых_навыков
Радиус диаграммы = 200 пикселей (в UI)
Центр = (x, y) = (300, 300)

Для i-го навыка:
    angle = i * angleStep * Mathf.Deg2Rad
    pointRadius = (skill.percent / 100f) * radius
    x = centerX + pointRadius * Mathf.Cos(angle)
    y = centerY + pointRadius * Mathf.Sin(angle)
```

### 4.3 Визуальные параметры

| Элемент | Цвет | Толщина | Прозрачность | Анимация |
|---------|------|---------|--------------|----------|
| Оси (линии от центра) | #888888 | 2px | 1.0 | — |
| Кольца (33%, 66%, 100%) | #444444 | 1px | 0.5 | — |
| Полигон (паутина) | #00AAFF | заполнение | 0.4 | плавное изменение |
| Контур полигона | #00AAFF | 3px | 1.0 | — |
| Точки на вершинах | #00AAFF | 8px диаметр | 1.0 | — |
| Золотая вершина (100%) | #FFD700 | 12px диаметр | 1.0 | пульсация |
| Текст названия навыка | #FFFFFF | шрифт 14 | 1.0 | — |

### 4.4 Техническая реализация

✅ **Текущая реализация:** собственный UGUI-компонент `RadarChartGraphic`.

**Почему так:**
- не нужен внешний ассет ради одной паутины;
- полный контроль над кольцами, осями, полигоном, outline и точками;
- проще поддерживать `ТОП-12`, анимацию и золотые вершины при `100%`;
- в проекте нет внешней chart-зависимости.

**Текущий stack:**
- `SkillsPanelUI` собирает `TOP-12` навыков;
- `RadarChartGraphic` рисует сетку, оси, паутину и точки;
- maxed-вершины подсвечиваются отдельно.

### 4.5 Обновление диаграммы

```csharp
public void UpdateRadarChart() {
    List<Skill> topSkills = skillSystem.GetTopSkills(12);
    var normalizedValues = topSkills.Select(skill => skill.percent / 100f).ToList();
    var markerColors = BuildPalette(topSkills.Count);
    radarChartGraphic.SetValuesAnimated(normalizedValues, markerColors, changedIndex);
}
```

---

## 5. Все UI экраны + флоу

### 5.1 Главный экран (HomePanel)

```
┌────────────────────────────────────────────────────────────┐
│ [❤️80] [⚡50] [😊70]                    │ 🪙150              │
├────────────────────────────────────────────────────────────┤
│                      ┌─────────────┐                       │
│                      │   (◕‿◕)     │ ← питомец (анимация)   │
│                      │    /|\       │   Idle/Happy/Sad      │
│                      └─────────────┘                       │
│              В трекинге: 6 навыков                         │
├────────────────────────────────────────────────────────────┤
│                   🕸️ ПАУТИННАЯ ДИАГРАММА                    │
│                         (ТОП-12)                           │
│                      ┌─────────────────┐                   │
│                      │    📐 85%        │                   │
│                      │      │           │                   │
│                      │ 💃60%─┼─💻40%   │                   │
│                      │      │           │                   │
│                      │    ⚽ 25%        │                   │
│                      └─────────────────┘                   │
│                    [🔄 Обновить диаграмму]                   │
├────────────────────────────────────────────────────────────┤
│   🧠 Фокус: [📐 Математика] [▼]    [▶️ СТАРТ]               │
├────────────────────────────────────────────────────────────┤
│ [🏠 Дом] [📋 Задания] [⚔️ Битва] [🕸️ Навыки] [🛒 Магазин]   │
└────────────────────────────────────────────────────────────┘
```

### 5.2 Экран навыков (SkillPanel) — подробно

- **Верх:** полноценная паутина (ТОП-12)
- **Центр:** ScrollRect с VerticalLayoutGroup
- **Каждый элемент навыка:**
  - Иконка (32x32)
  - Название + compact action `Type` — для смены archetype без изменения `totalSP`
  - ProgressBar (заполнение от 0 до 100%)
  - Текст процента (например "85%")
  - Кнопка 🎯 (быстрый фокус) — открывает FocusPanel с этим навыком
  - Кнопка 🗑️ (активна ТОЛЬКО если `totalSP == 0`)
- **Кнопка добавить навык:**
  - Popup: поле ввода (макс 20 символов) + archetype cards вместо raw icon-строк
  - выбор archetype сразу задаёт semantic-тип навыка и canonical fallback `icon`
  - цена: первые 3 навыка бесплатно, затем 50 монет за новый слот

### 5.3 Экран заданий (MissionPanel) — ДВА ТИПА

**Актуальный runtime-статус:** экран уже собран как полноценная screen-level вкладка. Внутри есть отдельные секции `Skill Missions` и `Routines`, выбор до 5 skill missions, focus-linked progress, create flow для своих миссий/рутин, claim flow и бонус `5/5`.

```
┌────────────────────────────────────────────────────────────┐
│ [❤️80] [⚡50] [😊70]              🧙5 │ 🪙150              │
├────────────────────────────────────────────────────────────┤
│                   📋 ЕЖЕДНЕВНЫЕ ЗАДАНИЯ                     │
│              Сброс в 05:00                                  │
├────────────────────────────────────────────────────────────┤
│  🎯 НАВЫКОВЫЕ ЗАДАНИЯ (выбери до 5)                        │
│  ┌────────────────────────────────────────────────────────┐│
│  │ ☑️ 30 мин Математика    → +30 🪙, +30 SP 📐           ││
│  │ ☐ 15 мин Танцы          → +15 🪙, +15 SP 💃           ││
│  │ ☐ 1 час Программирование → +60 🪙, +60 SP 💻          ││
│  └────────────────────────────────────────────────────────┘│
│                                                            │
│  💧 ПРОСТЫЕ РУТИНЫ (можно сколько угодно)                   │
│  ┌────────────────────────────────────────────────────────┐│
│  │ ☑️ 2 литра воды          → +10 🪙, +5 настроение      ││
│  │ ☐ 8 часов сна            → +15 🪙, +10 ухода          ││
│  │ ☐ Принять душ            → +5 🪙, +3 ухода            ││
│  │ ☐ День без сладостей     → +20 🪙, +5 настроение      ││
│  └────────────────────────────────────────────────────────┘│
│                                                            │
│  ✅ Выполнено навыковых: 2/5                               │
│  🎁 Бонус за все 5 навыковых: +50 🪙 + Волшебный сундук    │
├────────────────────────────────────────────────────────────┤
│              [➕ Создать своё]    [🎲 Случайное]            │
└────────────────────────────────────────────────────────────┘
```

### 5.4 Режим фокуса (FocusPanel)

**Актуальный runtime-статус:** полный core loop уже собран. Игрок может выбрать навык и длительность, запустить фокус, поставить на паузу, продолжить, отменить, завершить раньше, получить отдельный result screen и восстановить состояние после перезапуска.

- **Шаг 1:** выбор навыка (дропдаун или сетка иконок)
- **Шаг 2:** выбор длительности: кнопки 15 | 30 | 45 | 60 + кастомный ползунок 5–120 минут
- **Шаг 3:** большой таймер (шрифт 80, формат ЧЧ:ММ)

**Во время фокуса (fullscreen):**
- Кнопки: Пауза (останавливает таймер), Отмена (подтверждение, награда не выдаётся), Готово (завершить раньше — награда пропорционально)
- Фон: приглушённый, трек Lo-Fi
- Питомец анимирован (надевает очки)

**После завершения:**
- Экран награды: анимация монет и прогресса навыка
- Показ нового процента (например 21% → 24%)
- Питомец получает +5 к счастью

---

## 6. Экономика и баланс (математика)

### 6.1 Валюта
- **Монеты (Coins):** основная валюта, за фокус, задания, битвы, рутины
- **Кристаллы (Gems):** премиум валюта (будущая монетизация), за достижения и ежедневные серии

### 6.2 Стоимость всего (таблица)

| Действие / предмет | Цена (монеты) | Эффект |
|--------------------|---------------|--------|
| Яблоко | 50 | +20 ❤️ здоровья |
| Энергетик | 40 | +30 ⚡ энергии |
| Игрушка | 30 | +25 😊 настроения |
| Лечебное зелье | 80 | полное восстановление ❤️ |
| Воскрешение | 50 | hunger=50, energy=50, mood=30 |
| Новый слот навыка (4-й+) | 50 | +1 максимум навыков |
| Создание своей рутины (4-я+) | 20 | новая рутина в списке |
| Скин комнаты (обычный) | 200 | косметика |
| Скин питомца (редкий) | 800 | +5% к счастью |

### 6.3 Награды

| Действие | Монеты | Skill SP / progress | Доп. эффект |
|----------|--------|---------------------|-------------|
| Фокус 15 мин | 30 | +30 SP | отклик питомца |
| Фокус 30 мин | 60 | +60 SP | отклик питомца |
| Фокус 60 мин | 120 | +120 SP | отклик питомца |
| Навыковое задание | 15–60 | +15–60 SP | 0 |
| Рутина (вода, душ) | 5–10 | 0 | настроение / уход |
| Рутина (спорт, чтение) | 15–25 | +20–30 SP | настроение |
| Победа над боссом | 30–90 | 0 | guidance / progression check |

### 6.4 Баланс роста
- 100% навыка ≈ 8 часов чистого фокуса
- 6 навыков по 100% ≈ 2–3 месяца игры
- `pet level` не участвует в текущем балансе роста

---

## 7. Система питомца (Tamagotchi Core)

### 7.1 Параметры (расширенные)

| Параметр | Диапазон | Начальное | Падение | Восстановление |
|----------|----------|-----------|---------|----------------|
| Голод (Hunger) | 0–100 | 80 | -0.25% в минуту | +20 от яблока |
| Энергия (Energy) | 0–100 | 70 | -10 за атаку в битве | +1% в минуту, +30 от энергетика |
| Настроение (Mood) | 0–100 | 85 | -0.1% в минуту | +25 от игрушки, +5 после фокуса |
| Здоровье (Health) | 0–100 | 100 | -5/мин если голод=0 | +20 от яблока, полное от зелья |

### 7.2 Смерть и воскрешение

**Условия смерти:**
- Голод достиг 0 → здоровье падает на 5 каждую минуту
- Здоровье достигло 0 → питомец умирает

**Экран смерти:**
- Затемнение, грустная анимация питомца
- Кнопка «Воскресить за 50 монет»
- Если нет монет: «Заработай фокусом или заданием»

**После воскрешения:**
- Голод = 50
- Энергия = 50
- Настроение = 30
- Здоровье = 50

### 7.3 Оффлайн прогресс

- Сохраняем `lastActiveTime` при закрытии игры
- При открытии считаем пропущенные минуты: `(DateTime.UtcNow - lastActiveTime).TotalMinutes`
- Применяем падение голода и настроения
- Если питомец умер оффлайн — показываем экран смерти при загрузке

---

## 8. Система заданий — ДВА ТИПА (навыковые + рутины)

**Статус:** `Implemented`

**Текущая реализация:** daily mission generation и product-layer уже собраны: `difficulty plan -> eligible candidates -> personalization weights -> weighted pick -> composition repair -> final ordering`, единый claim flow, daily reset в `05:00 local`, отдельные блоки `Skill Missions` и `Routines`, create flow для своих заданий/рутин, бонус `5/5` и dev-only telemetry/debug snapshot.

**Что ещё впереди:** дальнейший визуальный polish, дополнительные mission widgets и архитектурная разгрузка UI glue.

### 8.1 Типы заданий

| Тип | Описание | Пример | Привязка к навыкам | Лимит в день |
|-----|----------|--------|-------------------|--------------|
| **Навыковое** | Требует фокус на конкретный навык | «30 мин математики» | Да | до 5 |
| **Рутина** | Простое действие без фокуса | «Выпить 2 литра воды» | Нет | безлимит |

### 8.2 Структура задания

```csharp
public class DailyMission {
    public string id;
    public MissionType type;          // SkillMission, Routine
    public string title;              // "30 мин математики" или "Выпить воду"
    public string skillId;            // только для SkillMission
    public int requiredMinutes;       // только для SkillMission
    public int coinsReward;
    public int xpReward;
    public float skillGainPercent;    // только для SkillMission
    public int energyReward;          // для рутин
    public int moodReward;            // для рутин
    public bool isCompleted;
    public bool isClaimed;
}

public enum MissionType {
    SkillMission,
    Routine
}
```

### 8.3 Предустановленные рутины (10 штук)

| Рутина | Награда |
|--------|---------|
| 2 литра воды | +10 🪙, +5 😊 |
| 8 часов сна | +15 🪙, +10 ⚡ |
| Принять душ | +5 🪙, +3 ухода |
| День без сладостей | +20 🪙, +5 😊 |
| Прочитать 15 страниц | +15 🪙, +20 SP 📖 |
| День спорта | +25 🪙, +30 SP ⚽ |
| Позвонить родным | +10 🪙, +10 😊 |
| Убраться в комнате | +10 🪙, +3 ухода |
| Помедитировать 10 мин | +8 🪙, +15 ⚡ |
| Запланировать день | +12 🪙, +5 😊 |

### 8.4 Создание своего задания

**Для навыкового задания:**
- Выбрать навык из существующих
- Выбрать длительность (15–120 минут)
- Награда рассчитывается автоматически по формуле фокуса

**Для рутины:**
- Название (макс 30 символов)
- Выбрать награду (монеты, уход/энергия, настроение, прогресс навыка)
- Стоимость создания: 0 монет (первые 3), затем 20 монет

### 8.5 Прогресс заданий

- **Навыковые задания:** прогресс увеличивается, когда игрок завершает фокус на нужный навык
- **Рутины:** игрок вручную отмечает выполнение (чекбокс), после подтверждения выдаётся награда
- Бонус за выполнение всех 5 навыковых заданий: +50 монет + Волшебный сундук

### 8.6 Сброс в 5:00

```csharp
public void CheckDailyReset() {
    DateTime now = DateTime.Now;
    DateTime lastReset = GetLastResetTime();
    if (now.Date > lastReset.Date || (now.Hour >= 5 && lastReset.Hour < 5)) {
        ResetAllMissions();
        SetLastResetTime(now);
    }
}
```

### 8.7 Текущая реализованная генерация daily set

- План сложности строится отдельно от выбора конкретных миссий.
- Кандидаты собираются по текущей доступности игрока.
- На кандидатов применяется personalization profile.
- Базовый набор собирается через weighted pick.
- После этого composition layer чинит конфликтные комбинации.
- Если strict-правила не сходятся, включается fallback chain с ослаблением ограничений.

### 8.8 Dev-only прозрачность generation

- Для каждого generation cycle собирается runtime-only debug snapshot.
- В snapshot попадают difficulty plan, candidate weights, personalization summary, repair events, fallback path и final set.
- В production gameplay это не меняет outcome и не лезет в UI.

---

## 9. Режим фокуса (таймер + награды)

**Статус:** `Implemented`

**Текущая реализация:** `FocusSystem + FocusCoordinator + FocusPanelUI` уже закрывают полный цикл `select -> start -> running/paused -> complete/cancel/finish early -> result`. Награда рассчитывается в системе, UI её только показывает. Состояние активного фокуса сохраняется и восстанавливается после перезапуска; если таймер истёк оффлайн, игрок сразу получает result flow.

**Что ещё впереди:** дальнейший visual/audio polish и архитектурная разгрузка orchestration из `GameManager`.

### 9.1 Flow

```
1. Пользователь открывает FocusPanel
2. Выбирает навык из дропдауна (все существующие навыки)
3. Выбирает длительность (15/30/45/60 или кастомный слайдер 5–120)
4. Нажимает [▶️ СТАРТ]
5. Открывается fullscreen-экран с таймером
6. Таймер идёт в реальном времени (Time.realtimeSinceStartup)
7. Пользователь может поставить на паузу или завершить раньше
8. По завершении: расчёт награды, показ экрана награды
```

### 9.2 Техническая реализация таймера

```csharp
public class FocusTimer : MonoBehaviour {
    private float remainingSeconds;
    private bool isRunning;
    private Coroutine timerCoroutine;
    
    public void StartTimer(int minutes) {
        remainingSeconds = minutes * 60;
        isRunning = true;
        timerCoroutine = StartCoroutine(TimerCoroutine());
    }
    
    private IEnumerator TimerCoroutine() {
        float lastTime = Time.realtimeSinceStartup;
        while (isRunning && remainingSeconds > 0) {
            float now = Time.realtimeSinceStartup;
            float delta = now - lastTime;
            lastTime = now;
            remainingSeconds -= delta;
            UpdateUI(remainingSeconds);
            yield return null;
        }
        if (remainingSeconds <= 0) OnTimerComplete();
    }
    
    public void Pause() { isRunning = false; }
    public void CompleteEarly() { OnTimerComplete(); }
}
```

### 9.3 Награда за фокус

```csharp
public FocusReward CalculateReward(float plannedDurationSeconds, float actualDurationSeconds, string skillId) {
    int plannedCoinsReward = ScaleByDuration(balanceConfig.baseFocusReward, plannedDurationSeconds);
    int coinsReward = ApplyCompletionRatio(plannedCoinsReward, actualDurationSeconds, plannedDurationSeconds);
    int skillSpReward = skillsSystem.CalculateSkillPointsFromFocusDuration(actualDurationSeconds);
    int petXpReward = 0; // removed from active product scope

    return new FocusReward(skillSpReward, coinsReward, petXpReward, 0f);
}
```

---

## 10. Магазин, инвентарь, валюта

### 10.1 Структура предмета

```csharp
public class ItemDefinition {
    public string id;
    public string name;           // "Яблоко"
    public string description;    // "Восстанавливает +20 здоровья"
    public string iconId;         // "🍎"
    public int price;
    public ItemType type;         // Food, Energy, Mood, Health, Revival, Cosmetic
    public int effectValue;
    public bool isConsumable;
}
```

### 10.2 Инвентарь

```csharp
public class Inventory {
    public Dictionary<string, int> items; // itemId → количество
    public List<string> ownedSkins;
    public string equippedSkin;
}
```

### 10.3 Магазин (ShopPanel)

- Вкладки: **Еда**, **Энергия**, **Настроение**, **Скины**, **Особое** (воскрешение, слоты навыков)
- Каждый товар: иконка, цена, кнопка «Купить»
- При покупке: проверка монет → уменьшить → добавить предмет в инвентарь

---

## 11. Боевая система — УМНЫЕ БОССЫ (сравнение паутин)

### 11.0 Battle design v1 (актуальный override)

Ниже по разделу ещё могут встречаться старые идеи про прямое сравнение каждой оси паутины игрока и босса с отдельными бонусами/штрафами к урону. Для `Battle v1` это **не** является основным балансным правилом.

Актуальная `v1`-модель:

- `SP = реальная сила навыка`
- `Level = UX и читаемая ступень прогрессии`
- `Boss Power` считается по **cumulative totalSP**, а не по цене одного перехода
- `Player Battle Power` считается не по всем навыкам подряд, а по `top 3 skills by axisPercent`
- паутина босса в `v1` используется как визуальный стиль, а не как основная математическая модель боя

#### Формула силы игрока

```text
Player Battle Power = average(totalSP of top 3 skills by axisPercent)
```

#### Формула силы босса

`Boss Power` соответствует суммарному `totalSP`, нужному для целевого уровня навыка.

| Босс | Target Level | Boss Power |
|------|--------------|-----------:|
| Boss 1 | Lv1 | 100 |
| Boss 2 | Lv2 | 260 |
| Boss 3 | Lv3 | 516 |
| Boss 4 | Lv4 | 926 |
| Boss 5 | Lv5 | 1582 |
| Boss 6 | Lv6 | 2632 |
| Boss 7 | Lv7 | 4312 |
| Boss 8 | Lv8 | 7000 |
| Boss 9 | Lv9 | 11300 |
| Boss 10 | Lv10 | 18180 |

#### Правило победы

```text
if Player Battle Power >= Boss Power
    player wins
else
    player loses
```

#### Почему это выбрано для v1

- не наказывает игрока за добавление новых навыков с нулевым прогрессом
- опирается на реальный `totalSP`, а не на декоративный процент
- хорошо ложится на уже внедрённую модель `SP -> Level -> Axis Percent`
- остаётся простой для первого vertical slice battle

#### Практические правила для реализации

- battle unlock имеет смысл открывать только при `минимум 3 tracked skills`
- tie-break у `top 3`:
  - сначала `axisPercent`
  - потом `totalSP`
  - потом `lastFocusDate`, если нужен стабильный deterministic order
- `Boss Power` лучше хранить в data table, а не вычислять вручную по месту

#### Статус старого дизайна ниже

Старые блоки ниже про `skillWeb`, per-axis damage modifiers и советы по слабым осям считать `v2/vFuture`-идеями. Они могут вернуться позже как дополнительный shape-bonus слой, но не как главный баланс `Battle v1`.

### 11.1 Новая логика — сравнение паутин

**Главная идея:** У каждого босса есть своя «паутина навыков» — набор навыков, в которых он силён или слаб.

- Если твой навык **выше**, чем у босса → ты наносишь **бонусный урон**
- Если твой навык **ниже**, чем у босса → босс наносит **дополнительный урон** тебе
- Босс показывает твои слабые места → мотивирует развивать паутину

### 11.2 Структура босса

```csharp
public class Boss {
    public string id;
    public string name;                    // "Математический дракон"
    public int maxHealth;
    public int currentHealth;
    public int baseDamage;
    public Dictionary<string, float> skillWeb; // skillId → процент
    public int rewardCoins;
    public int rewardXp;                  // legacy field, не используется в текущем v1
    public float itemDropChance;
    public string adviceMessage;
}
```

### 11.3 Примеры боссов для Battle v1

| Босс | Target Level | Boss Power | Совет игроку |
|------|--------------|-----------:|--------------|
| Boss 1 | Lv1 | 100 | «Собери хотя бы 3 tracked skills и подними их через Focus.» |
| Boss 3 | Lv3 | 516 | «Подтяни top-3 навыка, чтобы средняя боевая сила стала стабильнее.» |
| Boss 5 | Lv5 | 1582 | «Смотри на `Player Battle Power`, а не на общее количество слабых навыков.» |
| Boss 8 | Lv8 | 7000 | «Для этого тира уже важен сильный top-3 по `axisPercent` и `effectiveSP`.» |
| Boss 10 | Lv10 | 18180 | «Финальный бой ожидает maxed-профиль и высокий `effectiveSP` у топовых навыков.» |

### 11.4 Расчёт исхода боя

```csharp
public BattleOutcome ResolveBattle(float playerBattlePower, int bossPower) {
    return playerBattlePower >= bossPower
        ? BattleOutcome.Win
        : BattleOutcome.Loss;
}
```

### 11.5 Flow битвы

1. Игрок открывает `BattlePanel`
2. Выбирает босса из списка
3. Видит `Player Battle Power`, `Boss Power`, требование по энергии и reward preview
4. Нажимает [FIGHT]:
   - проверка `>= 3 tracked skills`
   - проверка энергии (`>= 10`)
   - списание энергии
   - сравнение `Player Battle Power` и `Boss Power`
5. Показывается result screen с наградой и guidance
6. Победа даёт монеты; `pet XP` не выдаётся

---

## 12. Комната питомца (кастомизация)

### 12.1 Слоты кастомизации

| Слот | Влияние | Варианты |
|------|---------|----------|
| Фон | Визуал | Лес, Космос, Замок, Пляж |
| Лежанка | +5% восстановление энергии | Простая, Уютная, Королевская |
| Миска | +10% эффективность еды | Деревянная, Каменная, Золотая |
| Постеры | +2 настроения в день | 5 видов |

### 12.2 Бонусы от комнаты

```csharp
public class RoomBonus {
    public float energyRegenBonus;
    public float foodEfficiencyBonus;
    public int dailyMoodBonus;
}
```

### 12.3 Product note

- комната в текущем продукте развивается через монеты и текущий tier комнаты;
- `pet level` не используется как unlock-гейт для room upgrade;
- это economy/cosmetic progression слой, а не отдельная XP-лесенка питомца.

---

## 13. Прогрессия и достижения

### 13.1 Статус pet progression

- `pet XP` и `pet level` выведены из активного product scope;
- они не используются для наград, unlock-ов, battle rewards и room upgrade;
- legacy-поля в save допускаются только для обратной совместимости;
- актуальная вертикаль прогресса игрока — `skills via SP`, монеты и care-state питомца.

### 13.2 Достижения (примеры)

| Достижение | Условие | Награда |
|------------|---------|---------|
| Первый шаг | Достичь 10% любого навыка | 20 монет |
| Мастер | Достичь 100% любого навыка | 100 монет + иконка |
| Полиглот | Иметь 5 навыков >50% | 150 монет |
| Фокусник | Суммарно 10 часов фокуса | 200 монет |
| Коллекционер | Добавить 20 навыков | 300 монет + скин |
| ЗОЖ-гуру | Выполнить 50 рутин | 100 монет |

---

## 14. Звук, музыка, анимации

### 14.1 Звуковые события

| Событие | Файл | Громкость |
|---------|------|-----------|
| Клик по кнопке | click.wav | 0.6 |
| Завершение фокуса | focus_complete.wav | 0.8 |
| Получение монет | coin.wav | 0.7 |
| Повышение уровня | level_up.wav | 0.9 |
| Победа в битве | victory.wav | 0.8 |
| Питомец голоден | sad_pet.wav | 0.5 |
| Кормление | eat.wav | 0.6 |
| Выполнение рутины | routine_complete.wav | 0.6 |

### 14.2 Анимации питомца (Animator Controller)

| Состояние | Триггер | Длительность |
|-----------|---------|--------------|
| Idle | (по умолчанию) | зациклена |
| Happy | SetTrigger("Happy") | 2 сек, затем Idle |
| Sad | SetTrigger("Sad") | 2 сек, затем Idle |
| Focus | SetTrigger("Focus") | зациклена |
| Attack | SetTrigger("Attack") | 0.5 сек, затем Idle |

### 14.3 Фоновая музыка

- Лобби (главный экран): спокойная Lo-Fi мелодия, зациклена
- Режим фокуса: инструментальная медитативная музыка
- Битва: быстрый чиптюн

---

## 15. Сохранения и оффлайн‑режим

**Статус:** `Implemented`

**Текущая реализация:** gameplay save/load работает через file-based JSON в `Application.persistentDataPath`. Есть temp/main/backup files, нормализация данных при загрузке и legacy migration из старого `PlayerPrefs` save.

**Что ещё впереди:** дополнительные resilience/polish улучшения и расширение схемы по мере появления новых продуктовых экранов.

### 15.1 Формат сохранения (JSON)

```json
{
  "version": 5,
  "lastSaveTime": "2026-04-08T15:30:00Z",
  "coins": 150,
  "crystals": 0,
  "petStats": {
    "hunger": 80, "energy": 70, "mood": 85, "health": 100,
    "level": 5, "xp": 450
  },
  "skills": [
    {
      "id": "skill_001",
      "name": "Математика",
      "icon": "MTH",
      "archetypeId": "logic",
      "totalSP": 425
    }
  ],
  "inventory": { "items": { "apple": 3, "energy_drink": 1 }, "ownedSkins": ["default"], "equippedSkin": "default" },
  "room": { "backgroundId": "forest", "bedId": "simple", "bowlId": "wooden" },
  "dailyMissions": [],
  "routines": [],
  "lastResetDate": "2026-04-08T05:00:00Z",
  "achievements": []
}
```

### 15.2 Когда сохранять

| Событие | Действие |
|---------|----------|
| Завершение фокуса | Сохранить |
| Покупка в магазине | Сохранить |
| Использование предмета | Сохранить |
| Завершение битвы | Сохранить |
| Выполнение задания | Сохранить |
| Выполнение рутины | Сохранить |
| Добавление/удаление навыка | Сохранить |
| При паузе приложения | Сохранить |
| Каждые 60 секунд | Фоновое автосохранение |

---

## 16. Спринты разработки (11 спринтов)

Ниже остаётся целевой план разработки, но фактический прогресс проекта уже идёт нелинейно:
- `Missions` и `Radar` ушли вперёд относительно части roadmap.
- `Battle`, `Room/Shop` product UI и архитектурная разгрузка ещё не догнали их по зрелости.

### Спринт 0 — Настройка (1 день)
- [ ] Установить Unity 6 (`6000.4.1f1`) или совместимую версию
- [ ] Создать репозиторий Git, .gitignore
- [ ] Настроить папки проекта
- [x] Подготовить собственный `RadarChartGraphic` без внешнего chart-ассета

### Спринт 1 — Foundation (3 дня)
- [x] Сцены Bootstrap, Main
- [x] GameBootstrap, GameManager
- [x] SaveData, SaveManager (JSON)
- [ ] EventBus
- [x] Навигация между панелями

### Спринт 2 — Pet Core (3 дня)
- [x] PetSystem (Hunger, Energy, Mood, Health)
- [x] Падение параметров в реальном времени
- [x] Оффлайн прогресс
- [x] Смерть и воскрешение
- [x] Инвентарь (базовый)

### Спринт 3 — Skills + Radar Chart (4 дня) — КРИТИЧЕСКИЙ
- [x] Skill Model, SkillSystem (без лимита)
- [x] Добавление/удаление навыков (удаление только 0%)
- [x] SkillPanel (скроллящийся список)
- [x] Radar Chart (ТОП-12) — полная реализация
- [x] Popup добавления навыка

### Спринт 4 — Focus System (4 дня)
- [x] FocusPanel (выбор навыка, длительности)
- [x] Таймер (корутина с unscaled time)
- [x] Пауза, досрочное завершение
- [x] Расчёт награды
- [x] Экран награды после фокуса
- [x] Применение прогресса навыка

### Спринт 5 — Missions + Routines (3 дня) — ОБНОВЛЁННЫЙ
- [x] MissionSystem
- [x] Два типа: SkillMission и Routine
- [x] Ежедневный сброс в 5:00
- [x] Выбор до 5 навыковых заданий
- [x] Рутины (10 предустановленных)
- [x] Создание своих заданий (навыковых и рутин)
- [x] Прогресс заданий
- [x] Бонус за все 5 навыковых заданий

### Спринт 6 — Shop + Inventory (в проекте, v1)
- [x] `ShopPanel` как отдельный screen-level таб
- [x] Покупка предметов
- [x] Использование и экипировка предметов из инвентаря
- [x] Интеграция с care-loop питомца без `pet XP`
- [ ] Дополнительный content polish по категориям/визуалу

### Спринт 7 — Battle System (в проекте, Battle v1)
- [x] `BattlePanel`
- [x] Roster боссов с визуальной паутиной и `Boss Power`
- [x] `Player Battle Power = average(top 3 skills by axisPercent using effectiveSP)`
- [x] Советы игроку, energy gate и reward preview/result UX
- [x] Награда за победу монетами, без `pet XP`
- [ ] Runtime polish, tuning и mobile QA

### Спринт 8 — Room + Cosmetics (частично завершён)
- [x] `RoomPanel` как отдельный screen-level таб
- [x] Upgrade-first vertical slice с save/load и shell integration
- [x] Базовые визуальные состояния комнаты и mood bonus от upgrade
- [ ] Более широкий cosmetic catalog / equip variety
- [ ] Дополнительный content polish комнаты

### Спринт 9 — Polish + Audio (3 дня)
- [ ] Звуки (клики, фокус, битва, рутины)
- [ ] Фоновая музыка
- [ ] Анимации питомца (Idle, Happy, Sad, Focus, Attack)
- [ ] Анимации UI (DOTween: панели, таймер, монеты)
- [ ] Достижения (простые)

### Спринт 10 — Mobile Build & Test (3 дня)
- [ ] Android build (APK/AAB)
- [ ] Тест на реальном устройстве (Android 10+)
- [ ] iOS build (если есть Mac)
- [ ] Исправление багов
- [ ] Профилирование производительности

---

## 17. Критерии приёмки (расширенные)

- [ ] Нет крашей при добавлении 50+ навыков
- [ ] Паутина перерисовывается при каждом изменении `axisPercent`, показывает ТОП-12
- [ ] При 100% навыка — золотая вершина и визуальная отметка maxed-состояния
- [ ] Нельзя удалить навык с ненулевым прогрессом (`totalSP > 0`)
- [ ] Фокус даёт предсказуемый прирост `SP`, а UI корректно показывает `delta SP`, `level` и `axisPercent`
- [ ] Питомец умирает при голоде 0, воскрешение работает
- [ ] Закрытие и открытие игры сохраняет все данные
- [ ] Ежедневные задания сбрасываются в 5:00
- [ ] Рутины можно отмечать, награда выдаётся
- [ ] Можно создать своё задание (навыковое и рутину)
- [ ] Битва показывает `Player Battle Power`, `Boss Power`, reward preview и guidance без legacy per-axis damage логики
- [ ] Битва расходует энергию, при энергии <10 нельзя атаковать
- [ ] Звуки и музыка работают (можно выключить в настройках)

---

## 18. Что НЕ делаем в v1 (но запишем в v2)

| Фича | Почему не сейчас | Запланировано в |
|------|------------------|-----------------|
| Облачные сохранения | Требует сервер и Firebase | v2.0 |
| Пуш-уведомления | Сложно с локальными на iOS | v1.5 |
| Социальные функции (лидерборды) | Требует сервер | v2.0 |
| Микрофон / голос | Сложно, узкая ниша | не планируется |
| Сложные 3D анимации | Дорого по производительности | никогда |
| Ивенты по дням недели | Дополнительный контент | v1.5 |
| Полноценная аналитика | Firebase Analytics | v1.5 |
| Монетизация (Ads/IAP) | Не нужна в первой версии | v2.0 |
| Перековка навыка (reforge) | Замена удаления за монеты | v1.5 |
| Поиск по навыкам | Доп. удобство | v1.2 |

---

## 19. Риски и компромиссы

| Риск | Решение |
|------|---------|
| Radar Chart тормозит при 100+ навыках | Показываем ТОП-12, остальные в списке |
| Игрок забудет покормить → смерть | Добавить локальное уведомление (v1.2) |
| Слишком медленный рост навыков | Тюнить `SP` награды и mood/care modifiers, не возвращая pet XP |
| Скучно без сюжета | Добавить короткие цитаты питомца при 100% навыка |
| Android build не проходит из-за размера | Сжать текстуры до 1024x1024, использовать ASTC |
| Игрок не понимает, зачем развивать паутину | Боссы наглядно показывают слабые места |
| Рутины кажутся скучными | Давать видимые награды (монеты, уход, настроение, skill SP) |

---

## 20. Монетизация (опционально для будущего)

> ⚠️ Не включать в v1, но спроектировать так, чтобы добавить без переписывания.

- **IAP:** Кристаллы (пакеты $0.99 – $19.99)
- **За что покупать:** эксклюзивные скины питомца, удаление рекламы, слоты навыков (после 10 бесплатных)
- **Реклама:** rewarded video за двойную награду после фокуса (опционально)

---

