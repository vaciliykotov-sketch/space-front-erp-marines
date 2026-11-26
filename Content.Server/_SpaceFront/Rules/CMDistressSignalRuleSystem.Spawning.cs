using Content.Server.GameTicking;
using Content.Server.RandomMetadata;
using Content.Server.Spawners.Components;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server._RMC14.Fog;
using Content.Server._RMC14.TacticalMap;
using Content.Shared.Access.Components;
using Content.Shared.Access;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;
using Content.Shared.Nutrition.Components;
using Content.Shared.Preferences;
using Content.Shared.Radio.Components;
using Content.Shared.Radio;
using Content.Shared.Roles;
using Content.Shared.Warps;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Bioscan;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Fax;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._RMC14.Intel.Tech;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Spawners;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Vendors;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._SpaceFront.Rules;
using Content.Shared._SpaceFront.Vendors;
using Robust.Shared.Audio;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;


namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private bool InitializeSquadWar(Entity<CMDistressSignalRuleComponent> rule, CMDistressSignalRuleComponent comp)
    {
        // Space Front: в начале раунда счётчик уникальных фракций начинается заново.
        _squadWarFactionCounter = 0;

        // Первый корабль — текущий грид со спавнерами (запоминаем до загрузки второго).
        if (comp.ShipOneGrid == null)
        {
            var first = EntityQueryEnumerator<SquadSpawnerComponent, TransformComponent>();
            while (first.MoveNext(out _, out _, out var xform))
            {
                if (xform.GridUid is { } grid)
                {
                    comp.ShipOneGrid = grid;
                    break;
                }
            }
        }

        // Планета.
        if (comp.SpawnPlanet && comp.XenoMap == null)
        {
            var planet = SelectRandomPlanet();
            _lastPlanetMaps.Enqueue(planet.Proto.ID);
            while (_lastPlanetMaps.Count > 0 && _lastPlanetMaps.Count > _mapVoteExcludeLast)
            {
                _lastPlanetMaps.Dequeue();
            }

            if (_mapLoader.TryLoadMap(planet.Comp.Map, out var mapNullable, out var grids) &&
                grids.Count > 0)
            {
        var map = mapNullable.Value;
        EnsureComp<RMCPlanetComponent>(map);
        EnsureComp<TacticalMapComponent>(map);

                // Настоящий грид планеты: тот, что на КАРТЕ планеты (прелоадеры живут
                // на GridPreloader Map и не имеют Transform — их брать нельзя).
                var planetMapId = Comp<MapComponent>(map).MapId;
                EntityUid? realGrid = null;
                var gridQuery = EntityQueryEnumerator<MapGridComponent, TransformComponent>();
                while (gridQuery.MoveNext(out var gridUid, out _, out var gridXform))
                {
                    if (gridXform.MapID == planetMapId)
                    {
                        realGrid = gridUid;
                        break;
                    }
                }

                comp.XenoMap = realGrid;
                comp.SquadWarPlanetMapId = planetMapId;
                _mapSystem.InitializeMap((map, map));
                _intel.RunSpawners();
            }
            else if (comp.SpawnPlanet && comp.XenoMap != null && comp.SquadWarPlanetMapId == null)
            {
                // Планету уже загрузил ванильный SpawnXenoMap (InitializeXenoMap раньше нас) —
                // сохраняем её MapId, чтобы CheckSquadWarState мог перепривязать грид после SetUpStation.
                if (TryComp<TransformComponent>(comp.XenoMap.Value, out _))
                    comp.SquadWarPlanetMapId = _transform.GetMapId(comp.XenoMap.Value);
            }
        }

        // Второй Алмаер (параллельная карта).
        if (comp.ShipTwoGrid == null &&
            _mapLoader.TryLoadMap(comp.SquadWarSecondShipMap, out var map2, out var grids2) &&
            grids2.Count > 0)
        {
            comp.ShipTwoGrid = grids2.First();
            EnsureComp<AlmayerComponent>(map2.Value);
            _mapSystem.InitializeMap((map2.Value, map2.Value));
        }

        SpawnSquads(rule);

        // Варп-точки призрака: помечаем ВСЕ точки каждого корабля префиксом команды.
        if (comp.ShipOneGrid is { } warpGridOne)
        {
            var warpOne = EnsureComp<WarpPointComponent>(warpGridOne);
            warpOne.Location = "Алмаер Д.Ч. — Центр";
            Dirty(warpGridOne, warpOne);

            var warpsQuery = EntityQueryEnumerator<WarpPointComponent, TransformComponent>();
            while (warpsQuery.MoveNext(out var warpId, out var warpComp, out var warpXform))
            {
                if (warpXform.GridUid != warpGridOne)
                    continue;

                if (warpComp.Location == null || warpComp.Location.StartsWith("ДЧ:"))
                    continue;

                warpComp.Location = $"ДЧ: {warpComp.Location}";
                Dirty(warpId, warpComp);
            }
        }

        if (comp.ShipTwoGrid is { } warpGridTwo)
        {
            var warpTwo = EnsureComp<WarpPointComponent>(warpGridTwo);
            warpTwo.Location = "Алмаер А.Б. — Центр";
            Dirty(warpGridTwo, warpTwo);

            var warpsQuery = EntityQueryEnumerator<WarpPointComponent, TransformComponent>();
            while (warpsQuery.MoveNext(out var warpId, out var warpComp, out var warpXform))
            {
                if (warpXform.GridUid != warpGridTwo)
                    continue;

                if (warpComp.Location == null || warpComp.Location.StartsWith("АБ:"))
                    continue;

                warpComp.Location = $"АБ: {warpComp.Location}";
                Dirty(warpId, warpComp);
            }
        }

        // Консоли тактической карты второго корабля помечаем маркером команды.
        if (comp.ShipTwoGrid is { } shipTwoGrid)
        {
            var computerQuery = EntityQueryEnumerator<TacticalMapComputerComponent, TransformComponent>();
            while (computerQuery.MoveNext(out var computerId, out _, out var xform))
            {
                if (xform.GridUid == shipTwoGrid)
                    EnsureComp<SquadWarShipTwoComponent>(computerId);
            }

            var userQuery = EntityQueryEnumerator<TacticalMapUserComponent, TransformComponent>();
            while (userQuery.MoveNext(out var userId, out _, out var xform))
            {
                if (xform.GridUid == shipTwoGrid)
                    EnsureComp<SquadWarShipTwoComponent>(userId);
            }

            // Консоли наблюдения (Overwatch) корабля АБ — показывают только отряды АБ.
            var overwatchQuery = EntityQueryEnumerator<OverwatchConsoleComponent, TransformComponent>();
            while (overwatchQuery.MoveNext(out var overwatchId, out _, out var owXform))
            {
                if (owXform.GridUid == shipTwoGrid)
                    EnsureComp<SquadWarShipTwoComponent>(overwatchId);
            }
        }

        // Безлимитные слоты всех ролей на обоих кораблях.
        MakeAllJobsUnlimited();

        // Немедленно убираем маркеры «естественного» тумана карты,
        // чтобы система тумана не удалила куски нашей линии после спавна.
        var earlyRemovers = EntityQueryEnumerator<RandomAnchoredRemoverComponent>();
        while (earlyRemovers.MoveNext(out var earlyRemoverId, out _))
        {
            Del(earlyRemoverId);
        }

        var earlySpawners = EntityQueryEnumerator<RandomAnchoredSpawnerComponent>();
        while (earlySpawners.MoveNext(out var earlySpawnerId, out _))
        {
            Del(earlySpawnerId);
        }

        // Линия тумана строится из CheckSquadWarState, как только появится
        // настоящий грид планеты (после SetUpStation он пересоздаётся).

        // Повторная чистка ролей через 5 секунд (страховка от переинициализации станции).
        Timer.Spawn(TimeSpan.FromSeconds(5), MakeAllJobsUnlimited);

        // Проверка полного уничтожения команды — раз в 10 секунд.
        var ruleOwner = MetaData(comp.Owner).EntityName;
        _ = ruleOwner;
        Timer.SpawnRepeating(TimeSpan.FromSeconds(10), () => CheckSquadWarElimination(comp), System.Threading.CancellationToken.None);

        // Музыка один раз после рассеивания тумана (30 минут + небольшой запас).
        Timer.Spawn(TimeSpan.FromSeconds(1805), () =>
        {
            if (Deleted(comp.Owner) || comp.SquadWarFogCleared)
                return;

            ClearSquadWarFog(comp);
        });

        // Таймер проверок состояния (контроль планеты и т.д.) запускаем сразу —
        // иначе ванильный цикл ждёт NextCheck, который в SquadWar никто не инициализирует.
        comp.NextCheck = Timing.CurTime;
        comp.CheckEvery = TimeSpan.FromSeconds(5);

        // Гарантированный цикл: CheckSquadWarState вызывается из системного Update
        // (см. CMDistressSignalRuleSystem.Update) — таймеры здесь не тикали.
        Log.Info("SquadWar diag: timers armed, state check will run from system Update");

        return true;
    }

    private void MakeAllJobsUnlimited()
    {
        // Роли с ограничением количества одновременных игроков.
        // Ограниченные роли: по одному на корабль (суммарно 2 на станцию).
        var limited = new Dictionary<string, int>
        {
            ["CMCommandingOfficer"] = 2,
            ["CMExecutiveOfficer"] = 2,
            ["CMSquadLeader"] = 4, // по одному на отряд
            ["CMQuartermaster"] = 2,
            ["CMChiefMP"] = 2,
            ["CMCMO"] = 2,
            ["CMChiefEngineer"] = 2,
        };

        var removedCount = 0;
        var stations = EntityQueryEnumerator<StationJobsComponent>();
        while (stations.MoveNext(out var stationId, out _))
        {
            foreach (var job in _prototypes.EnumeratePrototypes<JobPrototype>())
            {
                // SquadWar: скрытые (ксеносы), выжившие и ванильные роли полностью удаляем из станции.
                if (!job.IsCM || job.Hidden || job.ID.Contains("Survivor") || job.ID.StartsWith("CMXeno"))
                {
                    _stationJobs.RemoveJob(stationId, job.ID);
                    removedCount++;
                    continue;
                }

                if (limited.TryGetValue(job.ID, out var limit))
                    _stationJobs.TrySetJobSlot(stationId, job.ID, limit, createSlot: true);
                else
                    _stationJobs.MakeJobUnlimited(stationId, job.ID);
            }
        }

        Log.Info($"SquadWar diag: jobs cleaned, removed {removedCount} hidden/non-CM roles");
    }

    private void SpawnSquadWarFog(CMDistressSignalRuleComponent comp)
    {
        if (comp.SquadWarFogDone)
            return;

        Log.Info($"SquadWar diag: fog attempt — xenoMap={comp.XenoMap}, hasTransform={comp.XenoMap is { } xm && TryComp<TransformComponent>(xm, out _)}, lz1Pattern={comp.SquadWarLZ1}, lz2Pattern={comp.SquadWarLZ2}");

        if (comp.XenoMap is not { } planetGrid ||
            !TryComp<TransformComponent>(planetGrid, out _))
            return;

        var mapId = _transform.GetMapId(planetGrid);
        Log.Info($"SquadWar diag: fog — mapId={mapId}");

        // Ищем две зоны высадки команд по именам.
        Vector2? lz1 = null;
        Vector2? lz2 = null;
        var destQuery = EntityQueryEnumerator<DropshipDestinationComponent, TransformComponent>();
        while (destQuery.MoveNext(out var destId, out _, out var xform))
        {
            if (xform.MapID != mapId)
                continue;

            var destName = Name(destId);
            if (lz1 == null && destName.Contains(comp.SquadWarLZ1, StringComparison.OrdinalIgnoreCase))
                lz1 = xform.WorldPosition;
            else if (lz2 == null && destName.Contains(comp.SquadWarLZ2, StringComparison.OrdinalIgnoreCase))
                lz2 = xform.WorldPosition;
        }

        // Фолбэк: если двух именованных LZ нет (Корсат и т.п.) — берём ЛЮБЫЕ две точки высадки на карте.
        if (lz1 == null || lz2 == null)
        {
            var fallbackQuery = EntityQueryEnumerator<DropshipDestinationComponent, TransformComponent>();
            while (fallbackQuery.MoveNext(out var fallbackId, out _, out var fallbackXform))
            {
                if (fallbackXform.MapID != mapId)
                    continue;

                if (lz1 == null)
                    lz1 = fallbackXform.WorldPosition;
                else if (lz2 == null)
                {
                    lz2 = fallbackXform.WorldPosition;
                    break;
                }
            }
        }

        // Фолбэк 2: нет точек вообще — берём позицию любой закреплённой сущности
        // на гриде планеты как центр линии.
        if (lz1 == null || lz2 == null)
        {
            var anyQuery = EntityQueryEnumerator<TransformComponent>();
            Vector2? anyPos = null;
            while (anyQuery.MoveNext(out var anyUid, out var anyXform))
            {
                if (anyXform.MapID == mapId && anyXform.GridUid == planetGrid && anyXform.Anchored)
                {
                    anyPos = anyXform.WorldPosition;
                    break;
                }
            }

            anyPos ??= Vector2.Zero;
            lz1 = anyPos.Value + new Vector2(0, -20);
            lz2 = anyPos.Value + new Vector2(0, 20);
            Log.Warning($"SquadWar diag: fog fallback — line at {anyPos.Value} (no LZ on map)");
        }

        // Убираем «естественный» туман карты и его спавнеры — останется только наша линия.
        var naturalFogQuery = EntityQueryEnumerator<FogComponent, TransformComponent>();
        while (naturalFogQuery.MoveNext(out var fogId, out _, out var fogXform))
        {
            if (fogXform.MapID == mapId)
                QueueDel(fogId);
        }

        var spawnerQuery = EntityQueryEnumerator<RandomAnchoredSpawnerComponent, TransformComponent>();
        while (spawnerQuery.MoveNext(out var spawnerId, out _, out var spawnerXform))
        {
            if (spawnerXform.MapID == mapId)
                QueueDel(spawnerId);
        }

        var removerQuery = EntityQueryEnumerator<RandomAnchoredRemoverComponent, TransformComponent>();
        while (removerQuery.MoveNext(out var removerId, out _, out var removerXform))
        {
            if (removerXform.MapID == mapId)
                QueueDel(removerId);
        }

        var randomSpawnerQuery = EntityQueryEnumerator<RandomSpawnerComponent, TransformComponent>();
        while (randomSpawnerQuery.MoveNext(out var randomSpawnerId, out _, out var randomSpawnerXform))
        {
            if (randomSpawnerXform.MapID == mapId)
                QueueDel(randomSpawnerId);
        }

        var a = lz1.Value;
        var b = lz2.Value;
        var mid = (a + b) / 2f;
        var delta = b - a;
        var vertical = MathF.Abs(delta.X) >= MathF.Abs(delta.Y);

        // Строим туман от середины в обе стороны, пока есть пол.
        var spawned = 0;
        Vector2 lineStart;
        Vector2 lineEnd;
        if (vertical)
        {
            spawned += WalkFogDirection(comp, mapId, new Vector2(mid.X, mid.Y), new Vector2(0, 1), out var s1, out var e1);
            spawned += WalkFogDirection(comp, mapId, new Vector2(mid.X, mid.Y - 1), new Vector2(0, -1), out var s2, out var e2);
            lineStart = Vector2.Min(s1, s2);
            lineEnd = Vector2.Max(e1, e2);
            Log.Info($"SquadWar diag: fog spawned {spawned} walls vertical at X={mid.X}");
        }
        else
        {
            spawned += WalkFogDirection(comp, mapId, new Vector2(mid.X, mid.Y), new Vector2(1, 0), out var s1, out var e1);
            spawned += WalkFogDirection(comp, mapId, new Vector2(mid.X - 1, mid.Y), new Vector2(-1, 0), out var s2, out var e2);
            lineStart = Vector2.Min(s1, s2);
            lineEnd = Vector2.Max(e1, e2);
            Log.Info($"SquadWar diag: fog spawned {spawned} walls horizontal at Y={mid.Y}");
        }

        // Метки тумана на тактической карте (напрямую в компонент карты — работает на любой карте).
        if (spawned > 0 && _mapManager.TryFindGridAt(mapId, mid, out _, out var midGrid))
        {
            var mapEnt = _mapManager.GetMapEntityId(mapId);
            var dirStep = vertical ? new Vector2(0, 1) : new Vector2(1, 0);
            var cur = lineStart - dirStep; // одна лишняя метка спереди
            var blipKey = -1000000;
            var stepCount = 0;
            for (var i = 0; i < 1000; i++)
            {
                if (vertical)
                {
                    if (cur.Y > lineEnd.Y)
                        break;
                }
                else if (cur.X > lineEnd.X)
                {
                    break;
                }

                // Метки через клетку — чтобы не наслаивались друг на друга.
                if (stepCount % 2 == 0)
                {
                    var local = midGrid.WorldToLocal(cur);
                    _tacticalMap.AddPermanentMapBlip(mapEnt, new Vector2i((int)MathF.Round(local.X), (int)MathF.Round(local.Y)), blipKey--);
                }

                stepCount++;
                cur += dirStep;
            }

            // одна лишняя метка сзади
            var tailLocal = midGrid.WorldToLocal(lineEnd + dirStep);
            _tacticalMap.AddPermanentMapBlip(mapEnt, new Vector2i((int)MathF.Round(tailLocal.X), (int)MathF.Round(tailLocal.Y)), blipKey--);

            Log.Info($"SquadWar diag: fog map blips added from {lineStart} to {lineEnd}, count={-1000000 - blipKey}");
        }

        // Линия построена — повторных попыток не делаем, а пустоты заполняем чуть позже.
        comp.SquadWarFogDone = true;
        Timer.Spawn(TimeSpan.FromSeconds(5), () => FillFogGaps(comp));
        Log.Info("SquadWar diag: fog wall built successfully");
    }

    private void FillFogGaps(CMDistressSignalRuleComponent comp)
    {
        if (comp.XenoMap is not { } planetGrid)
            return;

        var mapId = _transform.GetMapId(planetGrid);

        // Собираем все блоки тумана на планете.
        var positions = new List<Vector2>();
        var fogQuery = EntityQueryEnumerator<FogComponent, TransformComponent>();
        while (fogQuery.MoveNext(out _, out _, out var fogXform))
        {
            if (fogXform.MapID == mapId)
                positions.Add(fogXform.WorldPosition);
        }

        if (positions.Count < 2)
            return;

        var minX = positions.Min(v => v.X);
        var maxX = positions.Max(v => v.X);
        var minY = positions.Min(v => v.Y);
        var maxY = positions.Max(v => v.Y);
        var vertical = maxX - minX <= maxY - minY;

        var filled = 0;
        if (vertical)
        {
            var midX = (minX + maxX) / 2f;
            for (var y = MathF.Ceiling(minY); y <= MathF.Floor(maxY); y++)
            {
                var pos = new Vector2(midX, y + 0.5f);
                if (positions.Any(v => MathF.Abs(v.X - pos.X) < 0.01f && MathF.Abs(v.Y - pos.Y) < 0.01f))
                    continue;

                Spawn(comp.SquadWarFogWall, new MapCoordinates(pos, mapId));
                filled++;
            }
        }
        else
        {
            var midY = (minY + maxY) / 2f;
            for (var x = MathF.Ceiling(minX); x <= MathF.Floor(maxX); x++)
            {
                var pos = new Vector2(x + 0.5f, midY);
                if (positions.Any(v => MathF.Abs(v.X - pos.X) < 0.01f && MathF.Abs(v.Y - pos.Y) < 0.01f))
                    continue;

                Spawn(comp.SquadWarFogWall, new MapCoordinates(pos, mapId));
                filled++;
            }
        }

        Log.Info($"SquadWar diag: fog gaps filled: {filled}");
    }

    private int WalkFogDirection(CMDistressSignalRuleComponent comp, MapId mapId, Vector2 start, Vector2 step,
        out Vector2 firstSpawned, out Vector2 lastSpawned)
    {
        var positions = new List<Vector2>();
        var pos = start;
        var pending = new List<Vector2>();
        firstSpawned = start;
        lastSpawned = start;

        for (var i = 0; i < 1000; i++)
        {
            pos += step;

            var hasFloor = false;
            if (_mapManager.TryFindGridAt(mapId, pos, out var gridId, out var grid))
            {
                var local = grid.WorldToLocal(pos);
                var coords = new EntityCoordinates(gridId, local);
                if (grid.TryGetTileRef(coords, out var tileRef) && !tileRef.Tile.IsEmpty)
                    hasFloor = true;
            }

            if (!hasFloor)
            {
                pending.Add(pos);
                if (pending.Count >= 3)
                    break; // край карты

                continue;
            }

            positions.AddRange(pending);
            pending.Clear();
            positions.Add(pos);
        }

        foreach (var spawnPos in positions)
        {
            Spawn(comp.SquadWarFogWall, new MapCoordinates(spawnPos, mapId));
        }

        if (positions.Count > 0)
        {
            firstSpawned = positions[0];
            lastSpawned = positions[0];
            foreach (var spawnPos in positions)
            {
                firstSpawned = Vector2.Min(firstSpawned, spawnPos);
                lastSpawned = Vector2.Max(lastSpawned, spawnPos);
            }
        }

        return positions.Count;
    }

    [Dependency] private readonly InventorySystem _inventory = default!;

    private int _squadWarFactionCounter;

    private TimeSpan GetTotalRolePlaytime(ICommonSession session)
    {
        if (!_playTimeManager.TryGetTrackerTimes(session, out var times))
            return TimeSpan.Zero;

        var total = TimeSpan.Zero;
        foreach (var (tracker, time) in times)
        {
            if (tracker == "Overall" || tracker == "Admin")
                continue;

            total += time;
        }

        return total;
    }

    private void ApplyTeamRadioChannels(EntityUid mob, string team)
    {
        if (!_inventory.TryGetSlotEntity(mob, "ears", out var headsetUid) ||
            !TryComp<EncryptionKeyHolderComponent>(headsetUid, out var holder))
        {
            return;
        }

        var teamAB = team == "АБ";
        holder.Channels.Clear();
        holder.Channels.Add(teamAB ? "MarineAlpha" : "MarineCharlie");
        holder.Channels.Add(teamAB ? "MarineBravo" : "MarineDelta");
        Dirty(headsetUid.Value, holder);

        var ev = new EncryptionChannelsChangedEvent(holder);
        RaiseLocalEvent(headsetUid.Value, ev);
    }

    private void OnSquadWarPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (TryGetActiveRule() is not { SquadWar: true } rule)
            return;

        var mob = ev.Mob;
        var session = ev.Player;

        // Метка команды (АБ/ДЧ) — гарантированно для ВСЕХ ролей, включая
        // не-морпехов (исполнительный офицер и т.п.) и повторные спавны.
        // Определяем по кораблю, на котором появился моб.
        if (!TryComp<SquadWarTeamComponent>(mob, out _))
        {
            var teamComp = EnsureComp<SquadWarTeamComponent>(mob);
            EnsureComp<TacticalMapUserComponent>(mob);
            EnsureComp<TacticalMapTrackedComponent>(mob);
            EntitySystem.Get<TacticalMapSystem>().SetLiveUpdate(mob, true);
            var spawnGrid = Transform(mob).GridUid;
            teamComp.Team = spawnGrid == rule.ShipTwoGrid ? "АБ" : "ДЧ";
            Dirty(mob, teamComp);

            if (spawnGrid == rule.ShipTwoGrid)
                rule.SquadWarHadABPlayer = true;
            else
                rule.SquadWarHadDCHPlayer = true;

            Log.Info($"SquadWar diag: team mark granted post-spawn — mob={mob}, grid={spawnGrid}, team={teamComp.Team}");
        }

        // Радио: у всех ролей только 2 канала своей команды, независимо от выданных ключей
        // (безусловно — работает и при респавне с уже имеющейся меткой).
        var radioTeam = TryComp<SquadWarTeamComponent>(mob, out var teamForRadio)
            ? teamForRadio.Team
            : Transform(mob).GridUid == rule.ShipTwoGrid ? "АБ" : "ДЧ";

        ApplyTeamRadioChannels(mob, radioTeam);

        // Space Front: уникальная IFF-фракция жетону — пули никого не пропускают.
        if (_inventory.TryGetSlotEntity(mob, "id", out var idUid) &&
            idUid != null &&
            TryComp<ItemIFFComponent>(idUid.Value, out var iffComp))
        {
            var factionIndex = (_squadWarFactionCounter++ % 1000) + 1;
            _gunIFF.SetIdFaction((idUid.Value, iffComp), $"FactionSquadWar{factionIndex}");
        }
        Timer.Spawn(TimeSpan.FromSeconds(5), () =>
        {
            if (!Deleted(mob))
                ApplyTeamRadioChannels(mob, radioTeam);
        });

        void ApplyExperience()
        {
            if (Deleted(mob))
                return;

            var total = GetTotalRolePlaytime(session);
            if (total > TimeSpan.Zero)
                _rank.SetOverallPlaytimeHours(mob, (int)total.TotalHours);
        }

        ApplyExperience();
        Timer.Spawn(TimeSpan.FromSeconds(5), ApplyExperience);
    }

    private void OnSessionPlayTimesUpdated(ICommonSession session)
    {
        if (TryGetActiveRule() is not { SquadWar: true })
            return;

        if (session.AttachedEntity is not { } mob || !HasComp<RankComponent>(mob))
            return;

        var total = GetTotalRolePlaytime(session);
        if (total > TimeSpan.Zero)
            _rank.SetOverallPlaytimeHours(mob, (int)total.TotalHours);
    }
}
