# Tamahabchi — Architecture Handoff

## 1. Что это за проект

`Tamahabchi` — это Unity-проект про развитие реальных навыков через care-loop питомца, focus sessions, missions и базовую экономику. На текущем этапе это уже не прототип из отдельных скриптов, а рабочее ядро с сохранениями, оффлайн-прогрессом, missions product-layer и screen-based UI shell.

Главная продуктовая идея:

- игрок заботится о питомце;
- делает focus-сессии на выбранных навыках;
- получает награды, прогресс навыков и отклик питомца;
- закрывает skill missions и routines;
- двигается через простую экономику и room progression.

Сейчас strongest parts:

- `Skills + Radar`
- `Focus core loop`
- `MissionSystem + mission UX`
- `Save/Load + offline recovery`

Части, которые ещё не выглядят как “завершённый продукт”:

- полноценный `Shop` screen
- полноценный `Room` screen
- battle/boss layer
- дальнейшая архитектурная разгрузка `GameManager`

---

## 2. Технологический срез

- Engine: `Unity 6` — `6000.4.1f1`
- Render pipeline: `URP`
- UI: `uGUI + TMP`
- Input: `Input System`
- Tests: `Unity Test Framework`
- Persistence: file-based JSON save в `Application.persistentDataPath`
- Доп. tooling: `com.coplaydev.unity-mcp` подключён и используется для runtime/editor automation

См.:

- [ProjectVersion.txt](/G:/Tamahabchi/ProjectSettings/ProjectVersion.txt)
- [manifest.json](/G:/Tamahabchi/Packages/manifest.json)

---

## 3. Структура репозитория

Корневые важные директории:

- `Assets/` — весь игровой проект
- `Assets/Scripts/` — основная кодовая база
- `Assets/Tests/Editor/` — EditMode tests
- `Assets/Scenes/` — сцены
- `Packages/` — пакеты Unity
- `ProjectSettings/` — настройки проекта
- `ROADMAP.md` — продуктовый roadmap, частично устарел относительно кода
- `ТЕХНИЧЕСКОЕ_ЗАДАНИЕ_v4.0.md` — продуктовое ТЗ

Структура `Assets/Scripts`:

- `Coordinators/`
- `Core/` — фактически роль core сейчас выполняет `Assets/GameManager.cs`
- `Data/`
- `Persistence/`
- `Systems/`
- `UI/`
- `Utils/`

Практический смысл этих слоёв:

- `Systems` — бизнес-логика и source of truth по отдельным доменам
- `Coordinators` — склеивают несколько systems в продуктовые потоки
- `UI` — экранный слой, не должен считать награды и правила
- `Persistence` — сохранение, нормализация, пути файлов
- `Data` — сериализуемые DTO/containers
- `GameManager` — текущий composition root и orchestration hub

---

## 4. Главный архитектурный принцип проекта

Проект сейчас живёт по модели:

`GameManager -> Systems -> Coordinators -> UI bindings`

Это важно понимать правильно:

- `GameManager` не просто singleton с состоянием, а точка сборки runtime
- `Systems` не знают про Unity UI
- `UI` не должна менять данные напрямую
- `Coordinators` реализуют продуктовые flows поверх нескольких systems

Это не идеальная “чистая архитектура”. Реальность проекта такая:

- доменные правила уже хорошо вынесены в `Systems`
- orchestration частично вынесен в `Coordinators`
- но `GameManager` всё ещё толстый и держит много glue-кода

Именно это новая команда должна сохранять:

- не тащить reward/progress-логику в UI
- не дублировать бизнес-логику в `GameManager`
- новые flows лучше добавлять через coordinator/system, а не через хаки в panel scripts

---

## 5. Runtime-композиция: кто за что отвечает

### 5.1 `GameManager`

Файл:

- [GameManager.cs](/G:/Tamahabchi/Assets/GameManager.cs)

Роль:

- startup lifecycle
- load/create state
- init systems
- init coordinators
- wire UI dependencies
- update loop
- save lifecycle
- публичный facade для UI

Ключевой startup flow:

1. `Start()`
2. `RunStartupLifecycle()`
3. `RunBootstrapLifecycle(...)`
4. `ResolveReferences()`
5. `LoadOrCreateState(...)`
6. `ApplyStateToRuntime()`
7. `InitializeRuntimeSystems()`
8. `InitializeCoordinators()`
9. `InitializeUiBindings()`
10. `ApplyDailyResetWindowIfNeeded(...)`
11. `ApplyPendingOfflineProgress()`
12. `SyncUiFromRuntime()`

Фактически `GameManager` — текущий composition root.

Важно:

- это центральная точка, через которую UI получает данные и команды;
- он поднимает events вроде `OnSkillsChanged`, `OnMissionsChanged`, `OnFocusResultReady`;
- он же сериализует весь state в `SaveData`.

### 5.2 Systems

#### `PetSystem`

Файл:

- [PetSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/PetSystem.cs)

Отвечает за:

- hunger drain
- mood decay
- energy/mood/hunger mutation
- death/revive
- priority pet state summary (`Normal`, `Hungry`, `Critical`, `Dead`, `Revived`)

#### `SkillsSystem`

Файл:

- [SkillsSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/SkillsSystem.cs)

Отвечает за:

- список навыков
- добавление/удаление навыка
- прогресс навыка от focus
- golden state на 100%
- XP bonus от golden skills

#### `FocusSystem`

Файл:

- [FocusSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/FocusSystem.cs)

Отвечает за:

- состояние focus-session
- realtime timer
- pause/resume/cancel/finish early
- snapshot/save/restore
- reward scaling по completion ratio

Это чистая система таймера и session state, без UI и без прямых side effects в skills/pet/currency.

#### `MissionSystem`

Файл:

- [MissionSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/MissionSystem.cs)

Отвечает за:

- daily missions
- skill missions
- routines
- selection до 5 активных skill missions
- progress update от focus
- claim flow
- custom mission / custom routine creation
- 5/5 bonus
- personalization signals
- daily reset и regeneration

Это один из самых насыщенных доменных модулей в проекте.

#### `CurrencySystem`

- базовые операции по монетам

#### `InventorySystem`

- item storage/consume/add

#### `ShopSystem`

- покупка предметов через `CurrencySystem + InventorySystem`

#### `ProgressionSystem`

- XP/level
- расчёт work reward / focus reward
- unlock gating для buy flow

### 5.3 Coordinators

#### `FocusCoordinator`

Файл:

- [FocusCoordinator.cs](/G:/Tamahabchi/Assets/Scripts/Coordinators/FocusCoordinator.cs)

Роль:

- продуктовый orchestration focus loop
- старт/пауза/резюм/отмена/early finish
- применение результата focus к:
  - skills
  - progression
  - pet
  - missions
  - currency
- построение `FocusSessionResultData`
- сохранение/restore focus state

Важно:

- `FocusSystem` только считает session state;
- `FocusCoordinator` превращает completion в реальные игровые последствия.

#### `CoreLoopActionCoordinator`

Файл:

- [CoreLoopActionCoordinator.cs](/G:/Tamahabchi/Assets/Scripts/Coordinators/CoreLoopActionCoordinator.cs)

Роль:

- work/feed/buy/room upgrade actions
- клейм auto-claimed routine rewards
- onboarding side effects
- feedback + save + UI refresh

#### `PetFlowCoordinator`

Файл:

- [PetFlowCoordinator.cs](/G:/Tamahabchi/Assets/Scripts/Coordinators/PetFlowCoordinator.cs)

Роль:

- dead/revive transitions
- forced stop focus при смерти питомца
- revive flow и его побочные эффекты

#### `SaveLifecycleCoordinator`

Файл:

- [SaveLifecycleCoordinator.cs](/G:/Tamahabchi/Assets/Scripts/Coordinators/SaveLifecycleCoordinator.cs)

Роль:

- thin wrapper над save lifecycle:
  - load
  - save
  - reset
  - application pause / quit hooks

---

## 6. Data model: что сериализуется

Главный save container:

- [SaveData.cs](/G:/Tamahabchi/Assets/Scripts/Data/SaveData.cs)

Он содержит:

- `PetData`
- `CurrencyData`
- `InventoryData`
- `ProgressionData`
- `SkillsData`
- `RoomData`
- `MissionData`
- `DailyRewardData`
- `OnboardingData`
- `FocusStateData`
- `lastSeenUtc`
- `lastResetBucket`

Это текущая truth-схема персистентного состояния.

Важно для новой команды:

- если меняете сериализуемые DTO, почти всегда нужно обновлять `SaveNormalizer`
- если меняете focus persistence, надо проверять:
  - `FocusStateData`
  - `SaveNormalizer`
  - `FocusCoordinator`
  - `FocusSystem`

---

## 7. Persistence и оффлайн-логика

### 7.1 Save pipeline

Файлы:

- [SaveManager.cs](/G:/Tamahabchi/Assets/Scripts/Persistence/SaveManager.cs)
- [SaveNormalizer.cs](/G:/Tamahabchi/Assets/Scripts/Persistence/SaveNormalizer.cs)
- [SavePaths.cs](/G:/Tamahabchi/Assets/Scripts/Persistence/SavePaths.cs)

Как работает сохранение:

- save пишется в JSON
- запись идёт через `temp -> main`
- предыдущий `main` копируется в `backup`
- при загрузке сначала читается `main`, потом `backup`
- есть миграция legacy `PlayerPrefs` save

Файлы на диске:

- `.../persistentDataPath/Saves/savegame.json`
- `.../persistentDataPath/Saves/savegame.tmp`
- `.../persistentDataPath/Saves/savegame.bak`

### 7.2 `SaveNormalizer`

Это критически важный файл.

Он:

- создаёт дефолты
- чинит null-списки
- мигрирует старые поля
- clamped values
- нормализует inventory, missions, focus state и timestamps

Новая команда должна рассматривать `SaveNormalizer` как обязательную часть любой data migration.

### 7.3 Offline progress

Offline-логика проходит через `GameManager`:

- вычисляется `pendingOfflineSeconds`
- применяется daily reset boundary
- применяется offline focus recovery
- нормализуется pet state

Особенно чувствительные места:

- focus session, завершившаяся оффлайн
- reset bucket в `05:00`
- восстановление `lastResult` после focus

---

## 8. UI architecture: как устроены экраны

### 8.1 Важное правило

UI здесь частично scene-based, частично runtime-built.

Это значит:

- в сцене уже есть корневые runtime roots
- внутри этих roots панели часто достраивают layout программно
- нельзя судить о UI только по scene hierarchy или только по коду

### 8.2 Основные UI-слои

Файлы:

- [HUDUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/HUDUI.cs)
- [AppShellUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/AppShellUI.cs)
- [FocusPanelUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/FocusPanelUI.cs)
- [SkillsPanelUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/SkillsPanelUI.cs)
- [MissionPanelUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/MissionPanelUI.cs)
- [MissionRowUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/MissionRowUI.cs)
- [RoomVisualController.cs](/G:/Tamahabchi/Assets/Scripts/UI/RoomVisualController.cs)
- [RadarChartGraphic.cs](/G:/Tamahabchi/Assets/Scripts/UI/RadarChartGraphic.cs)

### 8.3 Что живёт в сцене `Main`

Проверено через Unity MCP на текущей сцене.

Главные root objects:

- `Main Camera`
- `Global Light 2D`
- `Canvas`
- `EventSystem`
- `GameManager`

Внутри `Canvas` ключевые runtime roots:

- `HomeRoot`
- `SkillsUIRoot`
- `MissionUIRoot`
- `FocusPanelRoot`
- `ShellRuntimeRoot`

Что это значит practically:

- `HomeRoot` — базовый home HUD и pet presentation
- `SkillsUIRoot/SkillsPanel`
- `MissionUIRoot/MissionPanel`
- `FocusPanelRoot`
- `ShellRuntimeRoot/AppShellRoot` — нижняя навигация и screen context

### 8.4 `HUDUI`

Это home-layer и status-layer:

- top bar
- pet headline/status
- home actions
- dead overlay
- revive/work in dead state

Раньше тут же жил legacy `MissionsSummaryButton`. Он был удалён из сцены, чтобы оставить единственную точку входа в missions через shell.

### 8.5 `AppShellUI`

Это screen router нижнего уровня.

Его роль:

- переключать `Home / Skills / Missions / Shop / Room`
- открывать `Focus` как blocking screen
- держать текущий screen context
- синхронизироваться с pet dead/revived state

Важно:

- именно shell теперь должен быть единственным entry point для missions screen
- если новая команда захочет добавить новые экраны, делать это нужно через shell model, а не отдельными случайными кнопками в HUD

### 8.6 `SkillsPanelUI`

Функции:

- список навыков
- добавление навыка
- radar chart top skills
- feedback popup при gain

Сейчас это один из самых стабильных UI-модулей.

### 8.7 `FocusPanelUI`

Функции:

- selection state
- active focus state
- paused state
- result state
- cancel confirm

Важно:

- UI не считает reward
- reward/result приходит из focus logic

### 8.8 `MissionPanelUI`

Функции:

- full-screen missions tab
- sectioned layout:
  - `Skill Missions`
  - `5/5 Bonus`
  - `Routines`
- `Create`
- `Reroll` placeholder
- create popup

Важно:

- panel уже переведён из popup-card в screen-level layout
- старый HUD mission shortcut удалён
- логика миссий остаётся в `MissionSystem`, UI только отображает и отправляет команды

### 8.9 `MissionRowUI`

Функции:

- visual row для skill mission и routine
- product-oriented content hierarchy:
  - title
  - status
  - progress
  - reward + action

Последние изменения:

- карточка стала zone-based
- починен баг с left clipping через исправление scroll content width
- routine rows компактнее skill rows

---

## 9. Основные игровые циклы

### 9.1 Core home loop

`Home -> Feed / Work / Buy / Focus / Room interaction`

Поток:

- действие из HUD
- coordinator
- system mutation
- mission side effects
- save
- UI refresh

### 9.2 Focus loop

`Select skill -> Select duration -> Start -> Pause/Resume/Finish Early/Cancel -> Result`

Участвуют:

- `FocusPanelUI`
- `FocusCoordinator`
- `FocusSystem`
- `SkillsSystem`
- `MissionSystem`
- `PetSystem`
- `CurrencySystem`
- `ProgressionSystem`
- `SaveManager`

### 9.3 Mission loop

`Open Missions -> Track skill missions / complete routines -> focus updates progress -> claim rewards -> bonus`

Участвуют:

- `MissionPanelUI`
- `MissionRowUI`
- `MissionSystem`
- `FocusCoordinator`
- `CoreLoopActionCoordinator`

### 9.4 Pet survival loop

`time passes -> hunger/energy/mood degrade -> warnings -> dead -> revive`

Участвуют:

- `PetSystem`
- `PetFlowCoordinator`
- `HUDUI`
- `AppShellUI`

Когда pet dead:

- focus forcibly stops
- shell returns to home
- dead overlay becomes primary UX

---

## 10. Навигация и видимые entry points

Новая команда должна ориентироваться на такую продуктовую модель:

### Home

Содержит:

- pet status
- home actions
- top bar
- dead overlay when needed

### Skills

Доступ только через нижнюю вкладку shell.

### Missions

Доступ только через нижнюю вкладку shell.

Это важно:

- legacy `MissionsSummaryButton` больше не должен возвращаться
- повторные entry points в home HUD считаются UX debt

### Focus

Открывается как отдельный blocking screen, обычно из home/skills/missions flow.

### Shop / Room

Shell tabs есть, но их продуктовая наполненность ещё не доведена до того же уровня, что `Skills/Missions/Focus`.

---

## 11. Что новая команда должна знать про сцену и Editor

### 11.1 Основная рабочая сцена

- `Assets/Scenes/Main.unity`

### 11.2 Что важно не сломать

- `Canvas` содержит несколько runtime roots, часть UI достраивается кодом
- `GameManager` должен находить:
  - `HUDUI`
  - `FocusPanelUI`
  - `SkillsPanelUI`
  - `MissionPanelUI`
  - `AppShellUI`
  - `RoomVisualController`

Если rename/delete root objects в сцене, нужно проверять:

- `ResolveReferences()` в `GameManager`
- auto-resolve logic в `HUDUI`
- runtime builders в panel scripts

### 11.3 Почему проект может выглядеть “странно” при первом чтении

Потому что часть UI элементов:

- хранится в сцене
- часть создаётся кодом
- часть legacy-узлов скрывается/перестраивается runtime-логикой

Поэтому handoff для новой команды должен включать правило:

> Перед удалением UI-узла всегда проверять и scene hierarchy, и runtime builder script.

---

## 12. Tests и проверочный контур

Текущие EditMode tests:

- [FocusCoreLoopTests.cs](/G:/Tamahabchi/Assets/Tests/Editor/FocusCoreLoopTests.cs)
- [MissionUxTests.cs](/G:/Tamahabchi/Assets/Tests/Editor/MissionUxTests.cs)
- [TimeBoundaryTests.cs](/G:/Tamahabchi/Assets/Tests/Editor/TimeBoundaryTests.cs)

Что покрыто:

- reward scaling при early finish
- skill cap на 100%
- pause/resume restore
- offline completion restore
- mission focus progress
- claim flow
- routine completion
- 5/5 bonus
- reset boundary around `05:00`

Чего пока мало:

- UI tests
- scene smoke tests
- regression coverage на shell navigation
- automated tests на room/shop screens

---

## 13. Текущие сильные стороны архитектуры

- Systems уже достаточно хорошо отделены от UI
- Save pipeline уже file-based и безопаснее legacy PlayerPrefs-only подхода
- Focus loop собран end-to-end
- Mission layer уже productized, а не просто генератор данных
- Daily reset и offline restore уже встроены в основной runtime

---

## 14. Текущие архитектурные долги

### 14.1 Толстый `GameManager`

Это главный технический долг проекта.

Он до сих пор держит:

- bootstrap
- facade для UI
- update orchestration
- save orchestration
- mission UI sync
- focus open helpers
- room/feed/work flows partially via callbacks

Новая команда должна понимать:

- не стоит расширять `GameManager` бесконечно
- новые большие фичи лучше строить через `System + Coordinator + UI`, оставляя `GameManager` точкой сборки

### 14.2 Смешанный scene/runtime UI подход

Это не фатально, но создаёт риск:

- трудно понять, что рисуется из сцены, а что из кода
- легко оставить legacy UI object после рефакторинга

### 14.3 Частично устаревший roadmap

- `ROADMAP.md` полезен как продуктовая карта
- но часть технических статусов уже не совпадает с кодом

Использовать нужно так:

- ТЗ и код — источник истины по текущей реализации
- roadmap — источник истины по intended product direction

---

## 15. Рекомендации для новой команды: как заходить в проект

### Первая неделя

1. Прочитать:
   - [ARCHITECTURE_HANDOFF.md](/G:/Tamahabchi/ARCHITECTURE_HANDOFF.md)
   - [ТЕХНИЧЕСКОЕ_ЗАДАНИЕ_v4.0.md](/G:/Tamahabchi/ТЕХНИЧЕСКОЕ_ЗАДАНИЕ_v4.0.md)
   - [ROADMAP.md](/G:/Tamahabchi/ROADMAP.md)
2. Открыть `Main.unity`
3. Просмотреть:
   - `GameManager`
   - `HUDUI`
   - `AppShellUI`
   - `SkillsPanelUI`
   - `MissionPanelUI`
   - `FocusPanelUI`
4. Прогнать EditMode tests
5. Руками пройти:
   - Home
   - Skills
   - Missions
   - Focus start/complete
   - save/load after restart

### Что читать в первую очередь из кода

1. [GameManager.cs](/G:/Tamahabchi/Assets/GameManager.cs)
2. [FocusCoordinator.cs](/G:/Tamahabchi/Assets/Scripts/Coordinators/FocusCoordinator.cs)
3. [MissionSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/MissionSystem.cs)
4. [FocusSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/FocusSystem.cs)
5. [SaveManager.cs](/G:/Tamahabchi/Assets/Scripts/Persistence/SaveManager.cs)
6. [SaveNormalizer.cs](/G:/Tamahabchi/Assets/Scripts/Persistence/SaveNormalizer.cs)
7. [AppShellUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/AppShellUI.cs)
8. [HUDUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/HUDUI.cs)

---

## 16. Безопасные правила изменений

### Если меняете UI

- не считать reward/progress в UI
- не дублировать state machine в panel script
- проверять scene object + runtime-built object оба

### Если меняете data schema

- обновлять `SaveNormalizer`
- думать о миграции старых save

### Если меняете focus flow

- проверять:
  - active session
  - pause/resume
  - early finish
  - offline restore
  - result reopening after restart

### Если меняете missions

- не ломать `selected skill missions <= 5`
- не ломать reset at `05:00`
- не переносить mission logic в `MissionPanelUI`

---

## 17. Текущее состояние продукта для передачи

Передавать проект другой команде можно как:

> рабочий vertical slice с устойчивым core loop, сохранениями, миссиями, skill progression и shell-based screen navigation, но с оставшимся техническим долгом вокруг `GameManager`, частично смешанного UI assembly и незавершённых product screens `Shop/Room/Battle`.

Это важная формулировка, потому что она честная:

- проект уже playable
- ключевые loops уже есть
- но это ещё не “финальная архитектура”

---

## 18. Рекомендуемый следующий шаг для новой команды

Если команда приходит без контекста, самый рациональный план такой:

1. Не начинать с battle
2. Не переписывать весь проект сразу
3. Зафиксировать handoff build
4. Провести cleanup-аудит `GameManager`
5. Довести `Shop` и `Room` до уровня `Skills/Missions/Focus`
6. Только потом идти в крупный следующий vertical slice

---

## 19. Короткий TL;DR для лида новой команды

- `GameManager` — composition root, но толстый
- `Systems` — доменная логика, их надо сохранять source of truth
- `Coordinators` — правильное место для gameplay orchestration
- UI частично scene-based, частично runtime-built
- `Focus`, `Skills`, `Missions`, `Save/Load` уже рабочие и взаимосвязаны
- `Missions` теперь должны открываться только через нижний shell
- любые изменения в data/save должны идти через `SaveNormalizer`
- самый безопасный способ развивать проект — не ломать loops, а строить новые фичи вокруг существующей системной модели

