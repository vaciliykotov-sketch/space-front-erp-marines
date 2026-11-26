# PATCHES.md — реестр изменений Space Front относительно апстримов
База: RMC-14 @ a6037c602167db6f0a8c. Обновлено: 26.11.2026.

Правило: свой код — только в папках _SpaceFront (Content.Server/_SpaceFront, Content.Shared/_SpaceFront,
Content.Client/_SpaceFront, Resources/Prototypes/_SpaceFront, Resources/Maps/_SpaceFront,
Resources/Textures/_SpaceFront, Resources/Audio/_SpaceFront, Resources/ServerInfo/Guidebook/_SpaceFront,
Resources/Locale/ru-RU/_spacefront). Правки чужих файлов — только через события/наследование/частичные классы,
реестр ведётся ниже. Файлы _Wega (ERP/Interaction/ToysLeash/QueenCarry) — сторонняя сборка, НЕ трогаем.

## 1. Наши файлы (полностью в _SpaceFront)
### Сервер
- Content.Server/_SpaceFront/Respawn/RespawnSystem.cs, GhostRespawnCommand.cs — окно смерти призрака (30с, лобби).
- Content.Server/_SpaceFront/MapEditor/{ClearFogCommand,SquadWarTestCommand,SquadWarTestConsoleCommand}.cs — тест-команды.
- Content.Server/_SpaceFront/TTS/* (6 файлов) — TTS-сервер (было Corvax.TTS).
- Content.Server/_SpaceFront/Vehicle/RMCSpawnVehicleOnUseSystem.cs — спавн техники по использованию предмета.
### Shared
- Content.Shared/_SpaceFront/Respawn/{RespawnResetEvent,SquadWarLeaveNowEvent}.cs
- Content.Shared/_SpaceFront/Rules/{SquadWarTeamComponent,SquadWarShipTwoComponent,SquadWarTestMarionetteComponent}.cs
- Content.Shared/_SpaceFront/Evacuation/EvacuationSuccessfullyLaunchedEvent.cs
- Content.Shared/_SpaceFront/Marines/SkillGrantOnUseComponent.cs
- Content.Shared/_SpaceFront/Vendors/SquadWarFreeVendorComponent.cs
- Content.Shared/_SpaceFront/Vehicle/{RMCSpawnVehicleOnUseComponent,RMCSpawnVehicleOnOrderComponent}.cs
- Content.Shared/_SpaceFront/TTS/** (9 файлов), _SpaceFront/CCCVars/CCCVars.cs
### Клиент
- Content.Client/_SpaceFront/Respawn/* (окно смерти, 4 файла)
- Content.Client/_SpaceFront/TTS/* (6 файлов), _SpaceFront/Lobby/HumanoidProfileEditor.TTS.cs
### Ресурсы
- Resources/Prototypes/_SpaceFront/** — наши прототипы (TTS-голоса, краты, маяки техники, IFF-фракции,
  правила SpaceFrontRules, стена Алмаера, intel_removed, двери/шаттлы Авроры, инвентарь-шаблоны).
- Resources/Maps/_SpaceFront/{almayer_ab,almayer_dch}.yml + Shuttles/{aurora_ab,aurora_dch,stories_alamo,stories_normandy}.yml
- Resources/Textures/_SpaceFront/** — Аврора (NormandyBlack, AlamoWhite), текстуры королевы и пр.
- Resources/Audio/_SpaceFront/Music/{fog_clear,ss13_epsilon}.ogg
- Resources/ServerInfo/Guidebook/_SpaceFront/SpaceFrontRules.xml
- Resources/Locale/ru-RU/_spacefront/** — ВСЯ наша ru-RU локализация (4545 файлов; в апстриме ru-RU нет).

## 2. Правки ЧУЖИХ файлов (подлежат выносу в _SpaceFront — Фаза 3)

### 2.1 ВЫНЕСЕНО в _SpaceFront (26.11) — чужой файл откачен к апстриму
- Content.Shared/_RMC14/Rules/CMDistressSignalRuleComponent.cs — все SquadWar-поля теперь в
  Content.Shared/_SpaceFront/Rules/CMDistressSignalRuleComponent.SquadWar.cs (partial, namespace тот же).
- Content.Server/_RMC14/Rules/DistressSignal/CMDistressSignalRuleSystem.cs — SquadWar-инициализация, хендлеры
  и зависимости теперь в Content.Server/_SpaceFront/Rules/CMDistressSignalRuleSystem.SquadWar.cs (partial).
  Маркированные правки, оставшиеся в чужом файле (минимум):
  1) using Robust.Shared.Utility (для ResPath);
  2) вызов SquadWarInitialize() в Initialize();
  3) принудительная установка флагов RMC14SquadWar в TryGetActiveRuleEntity;
  4) ветка брифинга «rmc-squad-war-briefing» в ARES-анонсе.
- TryGetActiveRule остался приватным (как в апстриме); внешним вызывающим добавлен публичный
  аксессор CMDistressSignalRuleSystem.ActiveRule (partial); вызовы обновлены (MapEditor, Telephone, Dropship).

- Content.Server/_RMC14/Rules/DistressSignal/CMDistressSignalRuleSystem.Spawning.cs — SquadWar-спавн вынесен
  в Content.Server/_SpaceFront/Rules/CMDistressSignalRuleSystem.Spawning.cs (partial): InitializeSquadWar,
  MakeAllJobsUnlimited, SpawnSquadWarFog, FillFogGaps, WalkFogDirection, GetTotalRolePlaytime,
  ApplyTeamRadioChannels, OnSquadWarPlayerSpawned, OnSessionPlayTimesUpdated, _inventory, _squadWarFactionCounter.
  В чужом файле остались 4 целых метода с маркерами (OnRulePlayerSpawning, InitializeDropships,
  OnPlayerSpawning, SpawnSquads) + маркированный блок дополнительных using.

- Content.Server/_RMC14/Rules/DistressSignal/CMDistressSignalRuleSystem.{Helpers,Scuttle,Planet,Hijack,RoundEnd}.cs —
  вынесено в partial'ы Content.Server/_SpaceFront/Rules/CMDistressSignalRuleSystem.*.cs:
  Helpers → 8 членов (OnSquadWarAccessInit, OnSquadWarUIAccessInit, IsActivePlayer, CountActiveSquadMembers,
  CountShipPlayers, FilterSpawnersByGrid, FilterSquadWarSpawners и др.);
  RoundEnd → 11 членов (CheckSquadWarState, TryGetActiveRuleNewest, ClearSquadWarFog,
  CheckSquadWarElimination, EndSquadWarByElimination/Draw/Round, CheckSquadWarEnemyFlight, IsSquadWarTeamA и др.).
  В чужых файлах остались маркированные целые методы: Planet.SpawnXenoMap, Hijack.OnDropshipHijackLanded,
  Scuttle.{OnFusionReactorCanOverload,DetonateScuttle,UpdateScuttleRoundEnd,UnlockScuttle},
  Helpers.{GetSpawner,NextSquad}, RoundEnd.{OnRoundRestartCleanup,CheckRoundShouldEnd}.
- ЯДРО SquadWar полностью отделено: чужой CMDistressSignalRuleSystem*.cs = апстрим + маркированные
  целые методы (без вкраплений наших строк). Сборка 0 ошибок, смоук пройден, задеплоено на ТЕСТ.

### 2.2 ВЫНЕСЕНО (26.11, второй заход) — все оставшиеся C# файлы (85 шт.)
Каждый чужой файл = апстрим + маркированные целые члены/хунки (Stories-start/end) или
частичные классы в _SpaceFront (новые члены вынесены в partial-файлы рядом: _SpaceFront/<тот же путь>).
Охвачены: CMGhostSystem, DropshipSystem, TacticalMapSystem + SharedTacticalMapSystem, EvacuationSystem +
SharedEvacuationSystem, RankSystem, RequisitionsSystem, RMCTelephoneSystem, RMCLandmineSystem, VehicleSupplySystem,
GameTicker.{StatusShell,Player,RoundFlow,Spawning,Lobby}, DrowsinessSystem, RulesManager, ServerContentIoC,
EmergencyShuttleSystem, StationJobsSystem, VoiceMask{System,Component}, VoteManager, ChatManager.RateLimit,
ServerDbBase, Model, ModelSqlite, SharedInteractionSystem (SFDBG-диагностика), InventorySystem.{Equip,Slots},
SharedStrippableSystem (игрушки), SharedHumanoidAppearanceSystem + HumanoidAppearanceComponent (TTS-голос),
HumanoidCharacterProfile, ContentLocalizationManager, RadioChannelPrototype, VerbCategory, CCVars.*,
RMCAlertLevel{Component,System}, AreaSystem (CopyAreaGridColors), GhillieSuit{Component,System}, RMCCVars,
SharedRMCChemistrySystem, DropshipNavigationComputerComponent, SharedDropshipSystem, RMCLandmineComponent,
SharedRMCLandmineSystem, RMCHumanoidAppearanceSystem, SharedMarineControlComputerSystem, RankComponent,
SharedRankSystem, SkillsSystem, SquadSystem, MortarComponent, SharedNightVisionSystem,
SharedOverwatchConsoleSystem (командный фильтр консоли), SharedSentryTargettingSystem, TacticalMap* (3 компонента),
SharedRMCTelephoneSystem, SharedCMAutomatedVendorSystem + клиентские (RulesPopup, GhostGui, GhostUIController,
OptionsMenu, MiscTab, CharacterSetupGui, HumanoidProfileEditor, EntryPoint, ConstructionMenuPresenter,
Bui-классы _RMC14: DropshipNavigation, DropshipWeapons, MarineCommunicationsComputer, MarineControlComputer,
OverwatchConsole, Requisitions, RMCTelephone, CommendationsWindow).
Сборка Content.Server + Content.Client — 0 ошибок, смоук пройден (Ready, FATL нет).

Ресурсы:
- Resources/Maps/_RMC14/** (33 карты) — наши версии перенесены в Resources/Maps/_SpaceFront/**,
  чужие файлы откачены к апстриму; ссылки обновлены (прототипы планет/маркеры/шаттлы в _RMC14-файлах,
  CMDistressSignalRuleComponent.SquadWar.cs).
- Resources/Prototypes/_RMC14/** (1784 yml: роли, вендоры, виды, лобби, туман, двери, рецепты и др.) —
  ОСТАЮТСЯ изменёнными в чужих файлах: переопределение прототипов дублями ID запрещено движком
  (PrototypeLoadException «Duplicate ID» — проверено эмпирически), а перенос через parent с переименованием
  ID требует переписывания всех ссылок. Это ограничение движка, зафиксировано здесь. При мерже апстрима
  эти файлы разбираются вручную по списку выше.
- Resources/Locale/ru-RU — ВСЕ файлы наши (апстрим ru-RU не имеет); структура каталогов повторяет пути чужих
  файлов (для ясности соответствия), НО это наши добавления, а не правки.
- Resources/Textures/_RMC14/Decals/markings.rsi/cargo_white.png, Textures/Mobs/Species/Human/parts.rsi/eyelid-*.png,
  Textures/Interface/**/Slots/toys*.png — точечные добавления в чужих rsi (вынос невозможен без правки чужих meta).
- Resources/Prototypes/InventoryTemplates — наши 2 шаблона вынесены в _SpaceFront/InventoryTemplates.
Движок:
- RobustToolbox/** и RSI.NET/** — обновление движка 264.0.2→264.0.16 (сабмодуль; в архиве апстрима отсутствует,
  поэтому в диффе отображается как «добавлено» — это НЕ наши файлы, не трогать).

## 2.1. Подтверждения переноса (проверено программно 26.11)
- GameTicker.RoundFlow.cs, GameTicker.Spawning.cs, CMGhostSystem.cs, LobbyGui.xaml.cs —
  посленаправленное сравнение «истинное состояние (архив 25.11) vs эффективное (чужой файл без маркеров +
  partial-члены)»: потери членов НЕТ, расхождения только в границах атрибутов/doc-комментариев (косметика).
- Полная верификация всех 93 изменённых C#-файлов прогонялась; реальных потерь кода не выявлено.
- Замечание «апстрим-архив на сервере C:\RMCUpstream\rmc.zip не сравнён» — устарело: локальный апстрим
  RMC-14@a6037c60 распакован в D:\SS14\rmc_upstream и полный дифф сделан (ИНВЕНТАРИЗАЦИЯ_a6037c60.md).

## 2.2. Исправления 26.11 (вечер)
- Окно смерти: SetPositionLast (окно было под затемнением смерти) + сторож в FrameUpdate (UI-пересборка
  «тело → призрак» отсоединяла окно) + убрана кнопка «Роли призраков» (просьба владельца).
- CMIntelOfficer: роль разведки удалена прежним агентом целиком (джоб+шмот+доступы) — БД-профили падали с
  «invalid ProtoId». Вернул ID пустышкой в _SpaceFront/Roles/.../intel_officer.yml (setPreference: false,
  hideConsoleVisibility: true) — профили снова резолвятся, роль остаётся скрытой.
- ent-MobLizard/ent-MobCorgiSmart «Error extracting»: в апстриме этих ключей вообще нет — это НАШИ name-only
  записи без .desc. Добавлены .desc для всех записей animals.ftl без описаний. Остальные «Error extracting»
  (CM*/RMC*/Drink*) — предсуществующий шум, есть и на боевом, не трогаем.

## 3. Известные отклонения (осознанные)
- Content.Shared/Inventory/Events/ToysSlotDoAfterEvent.cs и Content.Shared/Strip/ToysStripEmptyDoAfterEvent.cs —
  наши события в ванильных путях; перенос требует правки _Wega-файлов (запрещено) — оставлены, зафиксировано здесь.
- Content.Shared/_SpaceFront/TTS/Events/VoiceMaskChangeVoiceMessage.cs — namespace оставлен Content.Shared.VoiceMask
  (сообщение ванильного UI).
- Content.Server/_SpaceFront/TTS/VoiceMaskSystem.TTS.cs — частичный класс ванильного VoiceMaskSystem
  (namespace Content.Server.VoiceMask — обязателен для partial).
- Content.Client/_SpaceFront/Lobby/HumanoidProfileEditor.TTS.cs — частичный класс HumanoidProfileEditor
  (namespace Content.Client.Lobby.UI — обязателен для partial).
- Content.Server/server_config.toml и BUILD.md — конфиг/документация, лежат в корне по соглашению.
- Content.Client/Lobby/UI/HumanoidProfileEditor.xaml.cs — удалены ссылки на несуществующий SpeciesButton
  (кнопка видов убрана ранее, сборка клиента была сломана; исправлено, маркер Stories-start).
- Локали: 147 ключей «Error extracting» — отсутствуют атрибуты .desc у RMC-сущностей (шум был и на боевом,
  файлы идентичны). Дубли ключей и сломанный admin-notes.ftl исправлены 26.11.

## 4. Порядок сборки/деплоя
- Сборка: dotnet clean Content.Server -c Release; dotnet build Content.Server -c Release --no-restore
  (и Content.Client аналогично). После сборки — D:\SS14\cleanup.ps1 (убить компиляторы).
- Деплой на тест (1214): scp DLL+ресурсы → POST /update с WatchdogToken → вотчдог поднимет.
- Боевой (1212) — только по явной просьбе владельца.
