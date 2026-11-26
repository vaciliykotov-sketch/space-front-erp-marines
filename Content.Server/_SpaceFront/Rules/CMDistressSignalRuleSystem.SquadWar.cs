using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Rules;
using Content.Server.Players.PlayTimeTracking;
using Robust.Server.GameObjects;
using Content.Server._RMC14.GameStates;
using Content.Server._SpaceFront.TTS;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Server.Shuttles.Events;
using Content.Shared._RMC14.Vendors;
using Content.Shared._SpaceFront.TTS;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Inventory.Events;
using Content.Shared._SpaceFront.Evacuation;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;
    [Dependency] private readonly RMCPvsSystem _rmcPvs = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly TTSSystem _tts = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeManager = default!;
    [Dependency] private readonly SharedCMAutomatedVendorSystem _automatedVendor = default!;
    [Dependency] private readonly SharedRankSystem _rank = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedTacticalMapSystem _tacticalMap = default!;

    private HashSet<ProtoId<AccessLevelPrototype>>? _squadWarAllAccess;

    /// <summary>
    /// Space Front: публичный доступ к активному правилу (в апстриме TryGetActiveRule приватный).
    /// Используется командами _SpaceFront/MapEditor.
    /// </summary>
    public CMDistressSignalRuleComponent? ActiveRule => TryGetActiveRule();

    private void SquadWarInitialize()
    {
        _playTimeManager.SessionPlayTimeUpdated += OnSessionPlayTimesUpdated;
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSquadWarPlayerSpawned);

        var foundProto2 = _prototypes.TryIndex(new EntProtoId("RMC14SquadWar"), out var squadWarProto2);
        Log.Info($"SquadWar diag: INIT TryIndex(RMC14SquadWar) = {foundProto2}");
        if (foundProto2 &&
            squadWarProto2!.Components!.TryGetComponent("CMDistressSignalRule", out var ruleCompRaw2) &&
            ruleCompRaw2 is CMDistressSignalRuleComponent ruleComp2)
        {
            Log.Info($"SquadWar diag: INIT PROTOTYPE data -> SquadWar={ruleComp2.SquadWar}, SpawnXenos={ruleComp2.SpawnXenos}, SecondShipMap={ruleComp2.SquadWarSecondShipMap}");
        }
        else
        {
            Log.Error("SquadWar diag: INIT component lookup FAILED");
        }

        SubscribeLocalEvent<EvacuationSuccessfullyLaunchedEvent>(OnEvacuationSuccessfullyLaunched);
        // SquadWar: двери остаются с ванильными ролевыми доступами, всё остальное открыто всем.
        SubscribeLocalEvent<AccessReaderComponent, MapInitEvent>(OnSquadWarAccessInit);
        SubscribeLocalEvent<ActivatableUIRequiresAccessComponent, MapInitEvent>(OnSquadWarUIAccessInit);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs ev)
    {
        var foundProto = _prototypes.TryIndex(new EntProtoId("RMC14SquadWar"), out var squadWarProto);
        Log.Info($"SquadWar diag: TryIndex(RMC14SquadWar) = {foundProto}");
        if (foundProto)
        {
            if (squadWarProto!.Components!.TryGetComponent("CMDistressSignalRule", out var ruleCompRaw) &&
                ruleCompRaw is CMDistressSignalRuleComponent ruleComp)
            {
                Log.Info($"SquadWar diag: PROTOTYPE data -> SquadWar={ruleComp.SquadWar}, SpawnXenos={ruleComp.SpawnXenos}, SpawnSurvivors={ruleComp.SpawnSurvivors}, SecondShipMap={ruleComp.SquadWarSecondShipMap}");
            }
            else
            {
                Log.Error("SquadWar diag: component lookup FAILED");
            }
        }
    }

    private void OnEvacuationSuccessfullyLaunched(EvacuationSuccessfullyLaunchedEvent ev)
    {
        var rule = TryGetActiveRule();
        if (rule is not { SquadWar: true })
            return;

        rule.SquadWarEvacSucceeded = true;

        // Определяем, чей корабль эвакуировался (по карте шлюпки в момент запуска).
        if (rule.ShipOneGrid is { } shipOne && rule.ShipTwoGrid is { } shipTwo)
        {
            var map = Transform(ev.Grid).MapUid;
            if (map == Transform(shipOne).MapUid)
                rule.SquadWarEvacShip = 2; // корабль ДЧ
            else if (map == Transform(shipTwo).MapUid)
                rule.SquadWarEvacShip = 1; // корабль АБ
        }

        Log.Info($"SquadWar: эвакуационная шлюпка успешно улетела (корабль {rule.SquadWarEvacShip})");
    }
}
