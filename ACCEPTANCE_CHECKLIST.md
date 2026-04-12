# Tamahabchi — Acceptance Checklist

Актуально на `2026-04-12`.

Этот документ фиксирует, что именно нужно прогнать перед следующим большим редизайном или новой крупной фичей.

## 0. Текущее состояние

На `2026-04-12` этот checklist больше не является пустым планом. Базовый acceptance-проход уже частично подтверждён:

- `Phase 1` подтверждён: shell navigation, panel transitions, pause/save flow и возврат в приложение ведут себя стабильно;
- `Phase 2` подтверждён: cold restart, save/load и offline apply не показали data-loss или double-apply;
- `Phase 3` не выявил product-blocker-ов по `Home`, `Skills`, `Focus`, `Missions`, `Battle`, `Shop`, `Room`;
- `Phase 4` не выявил критичных mobile/layout blocker-ов, а coverage усилен через `FocusPanelTests` и `HUDUITests`;
- post-cleanup regression подтверждён: полный `EditMode` прогон = `175/175 passed`;
- `Unity-MCP` в этой среде ненадёжен для `execute_code` в `play mode`, поэтому runtime acceptance сейчас опирается на сочетание editor-tests, edit-mode проверок и ручного screen-pass.

Практический смысл документа теперь такой:

- не удалять его после текущего прохода;
- использовать как regression-order перед большим UI-redesign или новой крупной feature-wave;
- heavy UI cleanup по `ShopPanelUI`, `SkillsPanelUI`, `HUDUI`, `FocusPanelUI`, `RoomPanelUI` и `MissionPanelUI` уже выполнен;
- после каждого крупного visual/UI-изменения возвращаться к этому же чеклисту, а не собирать новый с нуля.

## 1. Цель

Сейчас проекту нужен не новый system-layer, а подтверждение, что уже собранный vertical slice стабилен как продукт:

- `Home`
- `Skills`
- `Focus`
- `Missions`
- `Battle`
- `Shop`
- `Room`
- shell navigation
- save/load + offline/app resume

Практическое правило:

- пока этот checklist не пройден базово, не открывать новый большой gameplay-system;
- acceptance/runtime QA уже закрыт по ключевым фазам, но checklist остаётся активным regression-gate;
- следующий приоритет = visual/mobile polish и redesign prep на уже очищенной базе;
- только после этого открывать новый крупный продуктовый слой.

## 2. Release Gate

Acceptance считается пройденным, если выполнены все условия:

- нет data-loss сценариев после перезапуска приложения;
- нет soft-lock сценариев в shell navigation и panel overlays;
- нет “мертвых” CTA, которые визуально доступны, но не приводят к ожидаемому результату;
- все базовые flows работают после `pause/resume` и после холодного рестарта;
- save/load корректно сохраняет `coins`, pet state, active focus state, inventory/equipment, missions state, room progression;
- на small-screen/mobile layout нет критичных наложений, обрезанных CTA и недоступных кнопок;
- консоль Unity после acceptance-прогона не даёт новых repeatable ошибок.

Блокирующие баги для следующего этапа:

- потеря прогресса;
- broken navigation;
- зависающий overlay/popup;
- несоответствие economy state после save/load;
- mission/focus/battle result, который можно дублировать или ломать повторным входом;
- layout, из-за которого пользователь не может завершить основной flow.

## 3. Порядок работ

Порядок важен. Идём так:

1. shell + runtime lifecycle
2. save/load + offline recovery
3. screen acceptance по всем основным экранам
4. mobile layout pass
5. regression pass после фиксов

На текущем этапе фазы `1-4` уже базово пройдены. Этот порядок сохраняем как порядок повторного regression-pass после крупных UI-изменений.

## 4. Phase 1 — Shell And Runtime Lifecycle

### 4.1 Shell navigation

Прогон:

- открыть каждый основной экран из shell;
- переключаться по кругу: `Home -> Skills -> Focus -> Missions -> Battle -> Shop -> Room -> Home`;
- открыть экран повторно после возврата на `Home`;
- открыть popup внутри экрана, закрыть его, затем перейти на другой экран;
- быстро переключать экраны несколько раз подряд.

Acceptance:

- активен только один ожидаемый screen state;
- не остаются висящие overlay/popup после смены экрана;
- HUD и shell не дублируются;
- возврат на `Home` всегда приводит к стабильному состоянию.

Связанные automated tests:

- `AppShellNavigationTests`
- `GameUiShellCoordinatorTests`

### 4.2 App pause/resume and quit

Прогон:

- свернуть приложение на `Home`, затем вернуть;
- свернуть приложение во время активного `Focus`;
- свернуть приложение на экране `Missions`, `Battle`, `Shop`, `Room`;
- закрыть приложение во время обычного idle-состояния;
- закрыть приложение во время активного `Focus`, затем открыть заново.

Acceptance:

- приложение возвращается без soft-lock;
- активный `Focus` восстанавливается ожидаемо;
- save не ломается при `pause/quit`;
- UI после возврата отражает актуальный runtime state.

Связанные automated tests:

- `GameRuntimeLifecycleCoordinatorTests`
- `GameSaveLifecycleCoordinatorTests`
- `GamePersistenceUtilityTests`

## 5. Phase 2 — Save/Load And Offline Recovery

### 5.1 Cold restart

Прогон:

- сделать изменения состояния: купить предмет, экипировать skin, улучшить room, принять награду миссии, изменить pet state;
- закрыть приложение;
- открыть заново;
- проверить, что состояние восстановилось полностью.

Acceptance:

- `coins` совпадают;
- inventory и equipped skin совпадают;
- room upgrade state совпадает;
- mission state не откатился и не продублировался;
- pet state и `Home` отражают сохранённое состояние.

### 5.2 Offline progress

Прогон:

- выйти из приложения на idle-состоянии;
- выйти из приложения во время `Focus`;
- подождать реальный интервал;
- открыть приложение снова.

Acceptance:

- offline/app resume не создаёт дублирующие награды;
- pending offline progress применяется ровно один раз;
- daily reset window не ломает текущее состояние;
- UI после возвращения показывает уже нормализованный state.

Связанные automated tests:

- `SaveManagerTests`
- `GamePersistenceUtilityTests`
- `TimeBoundaryTests`

## 6. Phase 3 — Screen Acceptance

### 6.1 Home

Прогон:

- проверить базовое состояние pet summary;
- проверить onboarding/hint/runtime labels;
- перейти из `Home` во все основные экраны и вернуться;
- проверить состояние после изменения hunger/mood/coins/focus status.

Acceptance:

- `Home` всегда показывает актуальный pet/runtime state;
- onboarding/runtime copy не зависают в старом состоянии;
- CTA из `Home` ведут в корректные экраны.

Связанные automated tests:

- `HomeRuntimeUiCoordinatorTests`
- `HomeRuntimeUiPresenterTests`
- `HomeDetailsCoordinatorTests`
- `HomeDetailsPanelTests`

### 6.2 Skills

Прогон:

- добавить новый навык;
- выбрать навык;
- удалить навык;
- проверить radar и hero block;
- открыть экран несколько раз подряд;
- вернуться на экран после `Focus` и после mission rewards.

Acceptance:

- список навыков, выбор и hero state синхронны;
- radar не ломается при пустом/маленьком/большом списке;
- нет потерянного selection state;
- popup и feedback не застревают.

Связанные automated tests:

- `SkillProgressionModelTests`
- `SkillsPanelCoordinatorTests`
- `SkillsPanelPresenterTests`
- `SkillsPanelViewUtilityTests`
- `SkillsPanelTests`

### 6.3 Focus

Прогон:

- открыть `Focus`, выбрать навык и длительность;
- стартовать, поставить на паузу, продолжить;
- отменить;
- завершить досрочно;
- завершить успешно;
- перезапустить приложение во время активного `Focus`.

Acceptance:

- timer state устойчив;
- finish/cancel/pause не дублируют результат;
- награда и pet reaction применяются один раз;
- восстановление после рестарта работает ожидаемо.

Связанные automated tests:

- `FocusCoordinatorTests`
- `FocusCoreLoopTests`

### 6.4 Missions

Прогон:

- проверить daily generation;
- выбрать и завершить skill mission через реальный progress path;
- создать custom skill mission;
- создать routine;
- забрать награды;
- проверить 5/5 bonus;
- проверить daily reset boundary;
- проверить повторный вход на экран после claim/create.

Acceptance:

- `Skill Missions` и `Routines` отображаются в правильных секциях;
- generic dailies не попадают в ручные routines;
- `work`-миссии реально попадают в generation;
- claim flow не дублирует награды;
- bonus state и popup state не расходятся с model state.

Связанные automated tests:

- `MissionCoordinatorTests`
- `MissionPanelCoordinatorTests`
- `MissionPanelPresenterTests`
- `MissionPanelPopupBuilderTests`
- `MissionPanelViewUtilityTests`
- `MissionPanelTests`
- `MissionHudCoordinatorTests`
- `MissionHudPresenterTests`
- `MissionHudTests`
- `MissionUxTests`
- `TimeBoundaryTests`

### 6.5 Battle

Прогон:

- открыть roster;
- выбрать босса;
- проверить недоступный бой при недостатке энергии/условий;
- провести успешный бой;
- проверить результат и награды;
- вернуться на экран позже и перепроверить state.

Acceptance:

- boss selection и availability state стабильны;
- reward preview соответствует фактическому result;
- бой не даёт дублирующий reward;
- после боя shell/UI возвращаются в устойчивое состояние.

Связанные automated tests:

- `BattleSystemTests`
- `BattleCoordinatorTests`
- `BattlePanelPresenterTests`
- `BattlePanelTests`

### 6.6 Shop

Прогон:

- открыть категории;
- купить item;
- использовать consumable;
- экипировать skin;
- вернуться на экран после save/load;
- проверить недостаток валюты.

Acceptance:

- purchase/use/equip flows меняют state один раз;
- inventory и equipped state не расходятся;
- disabled state у недоступных товаров корректен;
- `coins` всегда совпадают с runtime/save state.

Связанные automated tests:

- `ShopRoomCoordinatorTests`
- `ShopPanelPresenterTests`
- `ShopPanelTests`

### 6.7 Room

Прогон:

- открыть `Room`;
- проверить текущий room state;
- выполнить upgrade;
- вернуться после save/load;
- проверить связку room state -> `Home`.

Acceptance:

- upgrade применяется один раз;
- визуальный state и runtime state совпадают;
- после перезапуска не происходит откат upgrade state.

Связанные automated tests:

- `RoomPanelStatePresenterTests`
- `RoomPanelTests`
- `ShopRoomCoordinatorTests`

## 7. Phase 4 — Mobile Layout Pass

Приоритетные экраны:

- `ShopPanelUI`
- `SkillsPanelUI`
- `FocusPanelUI`
- `RoomPanelUI`
- `MissionPanelUI`
- `BattlePanelUI`
- `HUDUI`

На каждом экране проверить:

- small-height layout;
- длинные локализованные строки;
- popup поверх keyboard/input;
- scrollability списков;
- доступность primary CTA без overlap;
- safe area / top bar / bottom spacing.

Acceptance:

- нет обрезанных CTA;
- нет критичных наложений текста и кнопок;
- popup и input реально доступны на мобильном размере;
- пользователь может завершить главный flow экрана без “пиксель-хантинга”.

## 8. Что делать после acceptance

Когда этот документ закрыт:

1. повторно проходить `Phase 3` и `Phase 4` после каждого крупного visual/UI-изменения как regression-gate
2. добивать visual/mobile polish для result/popup states в `Focus`, `Missions`, `Battle`
3. трогать `MissionPanelUI` и `MissionSystem.Generation.cs` только если это реально нужно под redesign или новые mission-фичи
4. только после этого открывать новый большой продуктовый слой или redesign wave

## 9. Практическая рекомендация на следующий рабочий цикл

Самый полезный следующий шаг:

- пройти короткий regression-pass по `Phase 3` и `Phase 4` уже после завершённого cleanup;
- добить visual/mobile polish для result/popup states в `Focus`, `Missions`, `Battle`;
- если нужен ещё один cleanup-проход перед редизайном, сначала проверять реальную product-нужду, а не открывать новый rewrite по инерции;
- все найденные баги делить на:
  - `Blocker`
  - `Before redesign`
  - `Can wait`

Это даёт нам не “ещё одну архитектурную идею”, а стабильный release/regression gate перед редизайном и следующей feature-wave.
