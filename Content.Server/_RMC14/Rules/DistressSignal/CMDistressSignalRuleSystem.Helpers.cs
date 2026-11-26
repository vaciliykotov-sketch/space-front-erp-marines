using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.Components;
using Content.Server.Spawners.Components;
using Content.Shared._RMC14.Item;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Thunderdome;
using Content.Shared._RMC14.WeedKiller;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction.FloorResin;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared.Coordinates;
using Content.Shared.Fax.Components;
using Content.Shared.Roles;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

// Stories-start: дополнительные using (SquadWar)
using Content.Shared.Access.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared._SpaceFront.Rules;
using System.Linq;
// Stories-end

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private const int FaxPowerLoadValue = 5;

    /// <summary>
    /// Checks whether a player is allowed to play the specified job, considering bans and playtime requirements.
    /// </summary>
    private bool IsJobAllowed(NetUserId id, ProtoId<JobPrototype> role)
    {
        if (!_player.TryGetSessionById(id, out var player))
            return false;

        var jobBans = _bans.GetJobBans(player.UserId);
        if (jobBans != null && jobBans.Contains(role))
            return false;

        return _playTime.IsAllowed(player, role);
    }

    // TODO RMC14 this would be literally anywhere else if the code for loading maps wasn't dogshit and broken upstream
    private void SpawnAdminAreas(CMDistressSignalRuleComponent comp)
    {
        bool SpawnMap(ResPath path, [NotNullWhen(true)] out EntityUid? mapEntityUid)
        {
            mapEntityUid = default;

            try
            {
                if (string.IsNullOrWhiteSpace(path.ToString()))
                    return false;

                if (!_mapLoader.TryLoadMap(path, out var map, out _))
                    return false;

                _mapSystem.InitializeMap((map.Value, map.Value));
                mapEntityUid = map;
                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Error loading {path} map:\n{e}");
            }

            return false;
        }

        foreach (var map in comp.AuxiliaryMaps)
        {
            SpawnMap(new ResPath(map), out _);
        }

        if (SpawnMap(comp.Thunderdome, out var mapEnt))
            EnsureComp<ThunderdomeMapComponent>(mapEnt.Value);
    }

    private void SetCamoType(CamouflageType? ct = null)
    {
        if (ct != null)
        {
            _camo.CurrentMapCamouflage = ct.Value;
            return;
        }

        if (SelectedPlanetMap != null)
            _camo.CurrentMapCamouflage = SelectedPlanetMap.Value.Comp.Camouflage;
    }

    /// <summary>
    /// Sets the hive of all loaded xeno friendly entities (e.g., weeds).
    /// Only makes sense for distress signal with 1 hive, with multiple hives you would need to determine which weeds belong to which hive
    /// </summary>
    private void SetFriendlyHives(EntityUid hive)
    {
        var query = EntityQueryEnumerator<XenoFriendlyComponent>();
        while (query.MoveNext(out var weeds, out _))
        {
            _hive.SetHive(weeds, hive);
        }

        var resinSlowdown = EntityQueryEnumerator<ResinSlowdownModifierComponent>();
        while (resinSlowdown.MoveNext(out var uid, out _))
        {
            _hive.SetHive(uid, hive);
        }

        var resinSpeedup = EntityQueryEnumerator<ResinSpeedupModifierComponent>();
        while (resinSpeedup.MoveNext(out var uid, out _))
        {
            _hive.SetHive(uid, hive);
        }

        var tunnelQuery = EntityQueryEnumerator<XenoTunnelComponent>();
        var tunnels = new List<EntityUid>();

        while (tunnelQuery.MoveNext(out var ent, out _))
        {
            tunnels.Add(ent);
        }
        
        foreach (var tunnel in tunnels)
        {
            // Replace all pre-mapped tunnels with a new tunnel with name and associated with the hive
            if (_xenoTunnel.TryPlaceTunnel(hive, null, tunnel.ToCoordinates(), out var newTunnel))
                RemCompDeferred<DeletedByWeedKillerComponent>(newTunnel.Value);

            QueueDel(tunnel);
        }
    }

    private void UnpowerFaxes(MapId map)
    {
        var faxes = EntityQueryEnumerator<FaxMachineComponent, ApcPowerReceiverComponent, TransformComponent>();
        while (faxes.MoveNext(out _, out var power, out var xform))
        {
            if (xform.MapID != map)
                continue;

            power.Load = FaxPowerLoadValue;
            power.NeedsPower = true;
        }
    }

    private bool IsChamberFull(EntityUid chamber)
    {
        if (!_hyperSleepChamberQuery.TryComp(chamber, out var hyperSleep))
            return false;

        return _containers.TryGetContainer(chamber, hyperSleep.ContainerId, out var container) &&
               container.Count > 0;
    }

    /// <summary>
    /// Collects and categorizes all squad-based spawn points by squad, job, and availability.
    /// </summary>
    private void CollectSquadSpawners(Spawners spawners)
    {
        var squadQuery = EntityQueryEnumerator<SquadSpawnerComponent>();
        while (squadQuery.MoveNext(out var uid, out var spawner))
        {
            if (IsChamberFull(uid))
            {
                if (spawner.Role == null)
                    spawners.SquadAnyFull.GetOrNew(spawner.Squad).Add(uid);
                else
                    spawners.SquadFull.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(uid);
                continue;
            }

            var found = TryFindAttachedChamber(uid, out var attachedChamber);
            if (found)
            {
                var isFull = IsChamberFull(attachedChamber);
                var target = isFull ? spawners.SquadAnyFull : spawners.SquadAny;
                var targetWithRole = isFull ? spawners.SquadFull : spawners.Squad;

                if (spawner.Role == null)
                    target.GetOrNew(spawner.Squad).Add(attachedChamber);
                else
                    targetWithRole.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(attachedChamber);
            }
            else
            {
                if (spawner.Role == null)
                    spawners.SquadAny.GetOrNew(spawner.Squad).Add(uid);
                else
                    spawners.Squad.GetOrNew(spawner.Squad).GetOrNew(spawner.Role.Value).Add(uid);
            }
        }
    }

    private bool TryFindAttachedChamber(EntityUid spawner, out EntityUid chamber)
    {
        chamber = default;
        foreach (var cardinal in _rmcMap.CardinalDirections)
        {
            var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(spawner, cardinal);
            while (anchored.MoveNext(out var anchoredId))
            {
                if (_hyperSleepChamberQuery.HasComp(anchoredId))
                {
                    chamber = anchoredId;
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Collects and categorizes all non-squad spawn points by job and availability.
    /// </summary>
    private void CollectNonSquadSpawners(Spawners spawners)
    {
        var nonSquadQuery = EntityQueryEnumerator<SpawnPointComponent>();
        while (nonSquadQuery.MoveNext(out var uid, out var spawner))
        {
            if (spawner.Job == null)
                continue;

            var target = IsChamberFull(uid)
                ? spawners.NonSquadFull.GetOrNew(spawner.Job.Value)
                : spawners.NonSquad.GetOrNew(spawner.Job.Value);

            target.Add(uid);
        }
    }

    private Spawners GetSpawners()
    {
        var spawners = new Spawners();
        CollectSquadSpawners(spawners);
        CollectNonSquadSpawners(spawners);
        return spawners;
    }

        // Stories-start: изменённый метод (наша версия, см. PATCHES.md)
    private (EntProtoId Id, EntityUid Ent) NextSquad(
        ProtoId<JobPrototype> job,
        CMDistressSignalRuleComponent rule,
        EntProtoId<SquadTeamComponent>? preferred)
    {
        // SquadWar: автоматическое равномерное распределение. Предпочтение игрока игнорируется,
        // лимитов ролей нет. Сначала балансируем КОРАБЛИ (команды), затем отряды внутри корабля.
        var shipOneTotal = CountShipPlayers(rule, rule.ShipOneGrid);
        var shipTwoTotal = CountShipPlayers(rule, rule.ShipTwoGrid);
        var preferShipTwo = shipTwoTotal < shipOneTotal;

        var candidates = new List<(EntProtoId SquadId, EntityUid Squad, int TotalMembers)>();
        foreach (var (squadId, squad) in rule.Squads)
        {
            if (!TryComp(squad, out SquadTeamComponent? team))
                continue;

            var squadEnt = new Entity<SquadTeamComponent>(squad, team);
            var total = CountActiveSquadMembers(squadEnt);
            candidates.Add((squadId, squad, total));
        }

        if (candidates.Count == 0)
        {
            foreach (var (squadId, squad) in rule.Squads)
                return (squadId, squad);
        }

        // Отряды на корабле с меньшим числом игроков — в приоритете.
        var open = candidates
            .Where(s => rule.SquadWarShipTwoSquads.Contains(s.SquadId) == preferShipTwo)
            .ToList();
        if (open.Count == 0)
            open = candidates;

        open.Sort((a, b) => a.TotalMembers.CompareTo(b.TotalMembers));

        var minTotal = open[0].TotalMembers;
        var best = open.Where(s => s.TotalMembers == minTotal).ToList();
        var chosen = _random.Pick(best);
        return (chosen.SquadId, chosen.Squad);
    }

    /// <summary>
    /// SquadWar: оставляет только спавнеры на «своём» корабле команды.
    /// </summary>
    /// <summary>
    /// SquadWar: у не-дверей доступы убираются при появлении — шкафы, ящики,
    /// консоли и кнопки открыты всем; двери остаются ванильными (ролевые доступы).
    /// </summary>
        // Stories-end
        // Stories-start: изменённый метод (наша версия, см. PATCHES.md)
    private (EntityUid Spawner, EntityUid? Squad)? GetSpawner(
        CMDistressSignalRuleComponent rule,
        JobPrototype job,
        EntProtoId<SquadTeamComponent>? preferred)
    {
        var allSpawners = GetSpawners();
        EntityUid? squad = null;

        if (job.HasSquad)
        {
            var (squadId, squadEnt) = NextSquad(job, rule, preferred);
            squad = squadEnt;

            if (allSpawners.Squad.TryGetValue(squadId, out var jobSpawners) &&
                jobSpawners.TryGetValue(job.ID, out var spawners))
            {
                return (_random.Pick(FilterSquadWarSpawners(rule, spawners, squadId)), squadEnt);
            }

            if (allSpawners.SquadAny.TryGetValue(squadId, out var anySpawners))
                return (_random.Pick(FilterSquadWarSpawners(rule, anySpawners, squadId)), squadEnt);

            if (allSpawners.SquadFull.TryGetValue(squadId, out jobSpawners) &&
                jobSpawners.TryGetValue(job.ID, out spawners))
            {
                return (_random.Pick(FilterSquadWarSpawners(rule, spawners, squadId)), squadEnt);
            }

            if (allSpawners.SquadAnyFull.TryGetValue(squadId, out anySpawners))
                return (_random.Pick(FilterSquadWarSpawners(rule, anySpawners, squadId)), squadEnt);

            Log.Error($"No valid spawn found for player. Squad: {squadId}, job: {job.ID}");

            if (allSpawners.NonSquad.TryGetValue(job.ID, out spawners))
                return (_random.Pick(FilterSquadWarSpawners(rule, spawners, null)), squadEnt);

            if (allSpawners.NonSquadFull.TryGetValue(job.ID, out spawners))
                return (_random.Pick(FilterSquadWarSpawners(rule, spawners, null)), squadEnt);

            Log.Error($"No valid spawn found for player. Job: {job.ID}");
        }
        else
        {
            // SquadWar: «пятый отряд» делится поровну между двумя Алмаерами.
            // Предпочитаем корабль, где меньше игроков пятого отряда; при равенстве — где меньше всего игроков.
            var shipTwoScore = rule.ShipTwoNonSquad;
            var shipOneScore = rule.ShipOneNonSquad;
            var toShipTwo = shipTwoScore < shipOneScore;
            if (shipTwoScore == shipOneScore)
            {
                var shipOneTotal = CountShipPlayers(rule, rule.ShipOneGrid);
                var shipTwoTotal = CountShipPlayers(rule, rule.ShipTwoGrid);
                toShipTwo = shipTwoTotal < shipOneTotal;
            }

            // Ограниченные роли: не более одной такой роли на корабль.
            if (LimitedPerShipRoles.Contains(job.ID))
            {
                var onOne = rule.ShipOneLimitedRoles.TryGetValue(job.ID, out var countOne) ? countOne : 0;
                var onTwo = rule.ShipTwoLimitedRoles.TryGetValue(job.ID, out var countTwo) ? countTwo : 0;

                if (onTwo == 0 && onOne > 0)
                    toShipTwo = true;
                else if (onOne == 0 && onTwo > 0)
                    toShipTwo = false;
                else if (onOne > 0 && onTwo > 0)
                    toShipTwo = shipTwoScore < shipOneScore; // обе заняты — фолбэк
                // иначе (обе свободны) — обычный баланс выше
            }

            var targetGrid = toShipTwo ? rule.ShipTwoGrid : rule.ShipOneGrid;

            if (allSpawners.NonSquad.TryGetValue(job.ID, out var spawners))
            {
                var picked = _random.Pick(FilterSpawnersByGrid(spawners, targetGrid));
                if (toShipTwo)
                {
                    rule.ShipTwoNonSquad++;
                    if (LimitedPerShipRoles.Contains(job.ID))
                        rule.ShipTwoLimitedRoles[job.ID] = rule.ShipTwoLimitedRoles.GetValueOrDefault(job.ID) + 1;
                }
                else
                {
                    rule.ShipOneNonSquad++;
                    if (LimitedPerShipRoles.Contains(job.ID))
                        rule.ShipOneLimitedRoles[job.ID] = rule.ShipOneLimitedRoles.GetValueOrDefault(job.ID) + 1;
                }

                return (picked, null);
            }

            if (allSpawners.NonSquadFull.TryGetValue(job.ID, out spawners))
            {
                var picked = _random.Pick(FilterSpawnersByGrid(spawners, targetGrid));
                if (toShipTwo)
                {
                    rule.ShipTwoNonSquad++;
                    if (LimitedPerShipRoles.Contains(job.ID))
                        rule.ShipTwoLimitedRoles[job.ID] = rule.ShipTwoLimitedRoles.GetValueOrDefault(job.ID) + 1;
                }
                else
                {
                    rule.ShipOneNonSquad++;
                    if (LimitedPerShipRoles.Contains(job.ID))
                        rule.ShipOneLimitedRoles[job.ID] = rule.ShipOneLimitedRoles.GetValueOrDefault(job.ID) + 1;
                }

                return (picked, null);
            }

            Log.Error($"No valid spawn found for player. Job: {job.ID}");
        }

        var pointsQuery = EntityQueryEnumerator<SpawnPointComponent>();
        var jobPoints = new List<EntityUid>();
        var anyJobPoints = new List<EntityUid>();
        var latePoints = new List<EntityUid>();

        while (pointsQuery.MoveNext(out var uid, out var point))
        {
            if (point.SpawnType == SpawnPointType.Job)
            {
                if (point.Job?.Id == job.ID)
                    jobPoints.Add(uid);
                else
                    anyJobPoints.Add(uid);
            }

            if (point.SpawnType == SpawnPointType.LateJoin)
                latePoints.Add(uid);
        }

        if (jobPoints.Count > 0)
            return (_random.Pick(jobPoints), squad);

        if (anyJobPoints.Count > 0)
            return (_random.Pick(anyJobPoints), squad);

        if (latePoints.Count > 0)
            return (_random.Pick(latePoints), squad);

        return null;
    }

    /// <summary>
    /// Container class for organizing spawn points by category (squad vs. non-squad, full vs. available).
    /// Used to efficiently manage and query spawn point availability during player spawning.
    /// </summary>
        // Stories-end
    private sealed class Spawners
    {
        // Squad spawners with available slots, organized by squad and job
        public readonly Dictionary<EntProtoId, Dictionary<ProtoId<JobPrototype>, List<EntityUid>>> Squad = new();
        // Squad spawners with any job slot, organized by squad
        public readonly Dictionary<EntProtoId, List<EntityUid>> SquadAny = new();
        // Full squad spawners with available slots, organized by squad and job
        public readonly Dictionary<EntProtoId, Dictionary<ProtoId<JobPrototype>, List<EntityUid>>> SquadFull = new();
        // Full squad spawners with any job slot, organized by squad
        public readonly Dictionary<EntProtoId, List<EntityUid>> SquadAnyFull = new();
        // Non-squad spawners with available slots, organized by job
        public readonly Dictionary<ProtoId<JobPrototype>, List<EntityUid>> NonSquad = new();
        // Full non-squad spawners, organized by job
        public readonly Dictionary<ProtoId<JobPrototype>, List<EntityUid>> NonSquadFull = new();
    }
}
