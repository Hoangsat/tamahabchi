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
- `Skill Archetype` semantic-layer поверх свободных навыков
- `Idle v1` status-first слой на `Home`: действия питомца по archetype, inbox и modest rewards без пассивного `SP`
- `Focus core loop`
- `MissionSystem + mission UX`
- `Save/Load + offline recovery`
- `Battle / Shop / Room` как уже рабочие v1 screen flows
- базовый acceptance-pass по shell, save/load и screen flows без найденных blocker-ов

Части, которые ещё не выглядят как “завершённый продукт”:

- последний подтверждённый полный `EditMode` прогон на cleanup-этапе = `175/175 passed`
- дальнейшая декомпозиция `MissionSystem.Generation.cs` по смысловым зонам
- mixed scene/runtime UI assembly
- visual/mobile polish по `Home` idle strip, result/popup states и финальным screen layouts

Практический execution-чеклист для acceptance вынесен отдельно в [ACCEPTANCE_CHECKLIST.md](/G:/Tamahabchi/ACCEPTANCE_CHECKLIST.md).

Текущее состояние acceptance: `Phase 1` и `Phase 2` подтверждены, `Phase 3` и `Phase 4` не выявили product-blocker-ов; heavy UI cleanup по `Shop/Skills/HUD/Focus/Room/Mission` уже выполнен, `Skill Archetype` слой уже интегрирован в `Skills`, `Idle v1` уже встроен в runtime/Home HUD, а следующий шаг — visual/mobile polish, content/icon art pass и runtime/playmode укрепление, а не новый system-layer.

### 1.0.1 Product Lock: pet XP / pet level removed from active scope

На `2026-04-11` инженерно фиксируем то же правило, что и в `ТЗ` / `ROADMAP`:

- `pet XP` и `pet level` не участвуют в активном gameplay loop;
- `Focus`, `Missions`, `Routines`, `Battle`, `Shop` и `Feed` не выдают опыт питомцу;
- `Room` upgrade и другие продуктовые unlock-гейты не используют уровень питомца;
- legacy-поля `progression.level` / `progression.xp` допускаются только для совместимости сейвов;
- legacy `rewardXp` в mission/battle runtime и старые XP-gain knobs больше не являются активным runtime-контрактом.

Если ниже встречаются старые упоминания `XP/level`, считать их историческими заметками, а не текущим источником истины.

### 1.1 Актуальная skill progression model (на 2026-04-11)

После последних изменений `Skills` больше не живут на старом `percent` как business truth.

Текущая runtime-модель:

- source of truth у навыка: `totalSP`
- вычисляемые значения:
  - `level`
  - `progressInLevel`
  - `requiredSPForNextLevel`
  - `progressInLevel01`
  - `axisPercent`
- progression table:
  - `100, 160, 256, 410, 656, 1050, 1680, 2688, 4300, 6880`
- уровни: `Lv.0..Lv.10`
- `axisPercent` считается по level bands:
  - каждый завершённый уровень = `10%`
  - текущий прогресс уровня заполняет следующий `10%` сегмент
  - maxed skill = `100%`
- `Golden` привязан к max level, а не к legacy `percent >= 100`
- `FocusCoordinator` и `MissionSystem` теперь работают со skill rewards в `SP`
- старое поле `percent` сохранено только как legacy migration field для сейвов
- поверх имени навыка теперь живёт semantic-layer `archetypeId`
- поле `icon` сохранено как canonical compatibility token (`MTH`, `ART`, `MSC` и т.д.), который продолжает кормить старый icon/UI путь
- `SaveNormalizer` обязан мигрировать старые навыки по схеме `icon -> archetypeId`, неизвестные значения идут в `general`

Практический смысл для новых изменений:

- не добавлять новую progression-логику в UI;
- не читать `skill.percent` как истинное значение прогресса;
- любые ranking/top-skill/radar вычисления должны идти через computed `axisPercent` из systems/read-model слоя.
- не завязывать баланс, формулы `SP` или mission rewards на `archetypeId` без отдельного продуктового решения.

### 1.1.1 Idle v1 (на 2026-04-13)

Что уже реально есть в коде:

- `IdleData` встроен в save schema; `SaveNormalizer.CurrentSaveVersion = 7`
- `IdleBehaviorSystem` выбирает действия каждые `20–35s`, использует top-3 навыка по `axisPercent` и `Skill Archetype`
- `IdleCoordinator` связывает `SkillsSystem`, `PetSystem`, `CurrencySystem`, `InventorySystem` и `RoomData`
- в `Critical / Neglected` состоянии idle продолжает показывать действия, но не генерирует награды
- live/offline idle создаёт только modest rewards: `coins`, `chest`, `moment`, `rare`
- idle не выдаёт `SP`, уровни, mission progress или pet XP
- `HUDUI` уже показывает компактный `Home` idle block: icon, action text, summary, badge и кнопку `Забрать`
- offline-pass capped: максимум `8h`, `1` opportunity каждые `15m`, максимум `4` новых события за возврат

---

### 1.2 ?????????? pet model (?? 2026-04-11)
???? ? handoff ??? ??????????? legacy-???????? death / revive ? energy ??? ????? runtime-core. ??? ???????? ???? ??? ??? ?? source of truth.
??????? runtime-??????:
- ???????? ?????????? state: Neglected
- ????:
  - hunger <= 0 && mood <= 0
- ?????:
  - hunger > 0 || mood > 0
- Dead / Revive ?????? ?? ???????? ???????? gameplay flow
- SkillDecaySystem ????????? +1 decayDebtSP ?? ?????? ?????? ??? neglect
- effectiveSP = max(0, totalSP - decayDebtSP) ???????????? ???:
  - radar
  - ranking ???????
  - battle power
- level ?????? ???????? ?? 	otalSP
- Focus ? neglect ???????????? ? ?? ????? ???? bypass ??? care-loop
- Energy ???????? ?????? ??? legacy save-field, ?? ?? ????????? ? core gameplay
???? ???? ??????????? ?????? ???????? ?????? ??????? ??? revive flow, ??????? ??? legacy-??????.

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

### 3.1 Files to trust for current skill progression and idle

Если нужно понять актуальную реализацию навыков и idle-слоя, смотреть в первую очередь сюда:

- [SkillProgressionModel.cs](/G:/Tamahabchi/Assets/Scripts/Systems/SkillProgressionModel.cs)
- [SkillArchetypeCatalog.cs](/G:/Tamahabchi/Assets/Scripts/Data/SkillArchetypeCatalog.cs)
- [SkillsSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/SkillsSystem.cs)
- [IdleData.cs](/G:/Tamahabchi/Assets/Scripts/Data/IdleData.cs)
- [IdleBehaviorSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/IdleBehaviorSystem.cs)
- [IdleCoordinator.cs](/G:/Tamahabchi/Assets/Scripts/Coordinators/IdleCoordinator.cs)
- [FocusCoordinator.cs](/G:/Tamahabchi/Assets/Scripts/Coordinators/FocusCoordinator.cs)
- [SaveNormalizer.cs](/G:/Tamahabchi/Assets/Scripts/Persistence/SaveNormalizer.cs)
- [GameRuntimeLifecycleCoordinator.cs](/G:/Tamahabchi/Assets/Scripts/Coordinators/GameRuntimeLifecycleCoordinator.cs)
- [SkillsPanelUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/SkillsPanelUI.cs)
- [SkillArchetypeCardUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/SkillArchetypeCardUI.cs)
- [FocusPanelUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/FocusPanelUI.cs)
- [HUDUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/HUDUI.cs)

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
- `GameManager` всё ещё крупный, но уже работает скорее как composition root/facade, а основной остаточный долг сместился в `MissionSystem.Generation.cs` и mixed scene/runtime UI assembly

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
- idle facade для `Home` HUD
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
- он поднимает events вроде `OnSkillsChanged`, `OnMissionsChanged`, `OnFocusResultReady`, `OnIdleChanged`;
- он же сериализует весь state в `SaveData`, включая `idleData` и pending idle inbox.

### 5.2 Systems

#### `PetSystem`

Файл:

- [PetSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/PetSystem.cs)

Отвечает за:

- hunger drain
- mood decay
- hunger/mood mutation
- neglected-state evaluation
- offline neglect accumulation
- priority pet state summary (`Normal`, `Hungry`, `Critical`, `Neglected`)

#### `IdleBehaviorSystem`

Файл:

- [IdleBehaviorSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/IdleBehaviorSystem.cs)

Отвечает за:

- выбор текущего idle-действия питомца
- top-3 skill selection по `axisPercent`
- archetype-driven action labels и icon token
- gate/cooldown/cap для idle-событий
- генерацию `coins / chest / moment / rare`
- capped offline-pass без дублирования событий

Важно:

- это source of truth для idle-логики;
- он не выдаёт `SP`, уровни или mission progress;
- `axisPercent` здесь используется только как read-model сигнал для richness/rarity, а не как новая progression-модель.

#### `SkillsSystem`

Файл:

- [SkillsSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/SkillsSystem.cs)

Отвечает за:

- список навыков
- добавление/удаление навыка
- archetype-aware создание навыка через `AddSkillWithArchetype(...)`
- compatibility wrapper старого `AddSkill(name, icon)`
- смену archetype у существующего навыка без потери `totalSP`
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

- legacy compatibility для `progression.level/xp`
- активный gameplay больше не опирается на pet XP / pet level
- не использовать как источник продуктовых reward/unlock правил

### 5.3 Coordinators

#### `IdleCoordinator`

Файл:

- [IdleCoordinator.cs](/G:/Tamahabchi/Assets/Scripts/Coordinators/IdleCoordinator.cs)

Роль:

- связывает `IdleBehaviorSystem` с `SkillsSystem`, `PetSystem`, `CurrencySystem`, `InventorySystem` и `RoomData`
- применяет claim-награды из inbox
- строит `IdleHomeView` для `HUDUI`
- решает reward blocking при `Critical / Neglected`

Важно:

- это bridge между idle runtime и остальной экономикой;
- именно он должен оставаться местом для room hooks и future idle modifiers, а не `HUDUI`.

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

- neglected-state transitions
- forced stop / unblock focus around neglect state
- care-first flow ? ??? ???????? ???????

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
- в текущей schema уже живут `Skill Archetype` и `IdleData`

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
- восстанавливает `skills[].archetypeId` из legacy `icon`, если archetype отсутствует или некорректен
- создаёт и нормализует `idleData`, `pendingEvents` и `collectedMomentIds`

Новая команда должна рассматривать `SaveNormalizer` как обязательную часть любой data migration.

### 7.3 Offline progress

Offline-логика проходит через `GameManager`:

- вычисляется `pendingOfflineSeconds`
- применяется daily reset boundary
- применяется offline focus recovery
- применяется capped idle offline-pass
- нормализуется pet state
- после apply обновляется anti-duplicate timestamp для idle

Особенно чувствительные места:

- focus session, завершившаяся оффлайн
- reset bucket в `05:00`
- восстановление `lastResult` после focus
- повторный вход без нового elapsed time не должен дублировать idle rewards

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
- archetype picker cards для создания навыка
- смена archetype у существующего навыка через compact `Type` action + popup
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

### 14.1 Остаточные hotspots после cleanup `GameManager`

`GameManager` всё ещё крупный, но после последних выносов он уже не главный и не самый токсичный долг проекта.

Текущие hotspots:

- `MissionSystem.Generation.cs` — тяжёлый generation/composition pipeline, который лучше дальше делить по смысловым зонам, а не по случайным helper-методам;
- `MissionPanelUI` уже разгружен до panel-flow/binding слоя; повторно открывать большой rewrite не нужно без новой продуктовой причины;
- mixed scene/runtime UI assembly — риск двойных объектов и legacy scene UI после будущего редизайна;
- result/popup visual polish в `Focus`, `Missions`, `Battle` — уже не архитектурный blocker, но следующая реальная зона продукта.

Практическое правило для новой команды:

- не возвращать orchestration обратно в `GameManager`;
- новые product flows строить через `System + Coordinator + Presenter/ViewUtility + UI`;
- не возвращать уже разгруженные `Shop/Skills/HUD/Focus/Room` обратно в монолитные panel scripts;
- оставшийся cleanup делать точечно, а не повторять большой rewrite ради формального идеала.

### 14.2 Смешанный scene/runtime UI подход

Это не фатально, но создаёт риск:

- трудно понять, что рисуется из сцены, а что из кода
- легко оставить legacy UI object после рефакторинга

### 14.3 Частично устаревающие продуктовые документы

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
   - `MissionSystem.cs` + `MissionSystem.Generation.cs`
   - `IdleBehaviorSystem.cs` + `IdleCoordinator.cs`
   - `HUDUI`
   - `AppShellUI`
   - `SkillsPanelUI`
   - `MissionPanelUI`
   - `FocusPanelUI`
   - `ShopPanelUI`
   - `RoomPanelUI`
4. Прогнать EditMode tests
5. Руками пройти:
   - Home
   - Idle claim на Home
   - Skills
   - Missions
   - Focus start/complete
   - save/load after restart
   - offline return с pending idle events

### Что читать в первую очередь из кода

1. [GameManager.cs](/G:/Tamahabchi/Assets/GameManager.cs)
2. [FocusCoordinator.cs](/G:/Tamahabchi/Assets/Scripts/Coordinators/FocusCoordinator.cs)
3. [IdleBehaviorSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/IdleBehaviorSystem.cs)
4. [IdleCoordinator.cs](/G:/Tamahabchi/Assets/Scripts/Coordinators/IdleCoordinator.cs)
5. [MissionSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/MissionSystem.cs)
6. [MissionSystem.Generation.cs](/G:/Tamahabchi/Assets/Scripts/Systems/MissionSystem.Generation.cs)
7. [FocusSystem.cs](/G:/Tamahabchi/Assets/Scripts/Systems/FocusSystem.cs)
8. [SaveManager.cs](/G:/Tamahabchi/Assets/Scripts/Persistence/SaveManager.cs)
9. [SaveNormalizer.cs](/G:/Tamahabchi/Assets/Scripts/Persistence/SaveNormalizer.cs)
10. [AppShellUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/AppShellUI.cs)
11. [HUDUI.cs](/G:/Tamahabchi/Assets/Scripts/UI/HUDUI.cs)

---

## 16. Безопасные правила изменений

### Если меняете UI

- не считать reward/progress в UI
- не дублировать state machine в panel script
- проверять scene object + runtime-built object оба

### Если меняете data schema

- обновлять `SaveNormalizer`
- думать о миграции старых save
- если меняете skill archetypes / icon mapping, синхронно обновлять `SkillArchetypeCatalog` и migration-логику в `SaveNormalizer`
- если меняете idle schema или reward payload, синхронно проверять `IdleData`, `IdleBehaviorSystem`, `IdleCoordinator`, `HUDUI` и offline-ветку в `GameManager`

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

> рабочий vertical slice с устойчивым core loop, сохранениями, миссиями, skill progression и shell-based screen navigation, где `Battle`, `Shop` и `Room` уже существуют как v1 screen flows, heavy UI cleanup по ключевым экранам уже выполнен, `EditMode` regression подтверждён (`175/175 passed`), а основной оставшийся техдолг живёт в mixed scene/runtime UI assembly и частично монолитном `MissionSystem.Generation.cs`.

Это важная формулировка, потому что она честная:

- проект уже playable
- ключевые loops уже есть
- но это ещё не “финальная архитектура”

---

## 18. Рекомендуемый следующий шаг для новой команды

Если команда приходит без контекста, самый рациональный план такой:

1. Не открывать новый большой vertical slice поверх текущей базы
2. Не переписывать весь проект сразу
3. Зафиксировать handoff build
4. Пройти regression/mobile QA по `Skills`, `Focus`, `Missions`, `Battle`, `Shop`, `Room`, `Home` и `Idle`
5. Добить visual/mobile polish по `Home` idle strip и result/popup states; `MissionPanelUI` трогать дальше только если появится новая продуктовая причина
6. Усилить runtime/playmode уверенность именно вокруг `Idle`, `Focus restore`, `Shop`, `Battle` и `offline return`
7. Только потом идти в новый крупный vertical slice или redesign wave

---

## 19. Короткий TL;DR для лида новой команды

- `GameManager` — composition root, но уже не единственный hotspot
- `Systems` — доменная логика, их надо сохранять source of truth
- `Coordinators` — правильное место для gameplay orchestration
- UI частично scene-based, частично runtime-built, и именно mixed assembly здесь остаётся самым большим практическим долгом
- `Focus`, `Skills`, `Missions`, `Save/Load` уже рабочие и взаимосвязаны
- `Idle v1` уже встроен: он отражает прогресс, но не конкурирует с `focus` и не выдаёт `SP`
- `Battle`, `Shop` и `Room` уже есть как рабочие v1 flows, их не нужно “изобретать заново”
- `Missions` теперь должны открываться только через нижний shell
- любые изменения в data/save должны идти через `SaveNormalizer`
- самый безопасный способ развивать проект — не ломать loops, а строить новые фичи вокруг существующей системной модели
