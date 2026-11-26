using Content.Server.GameTicking;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Shuttles.Events;
using Content.Shared.CCVar;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Preferences;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Armor.Ghillie;
using Content.Shared._RMC14.Armor.ThermalCloak;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Evacuation;
using Content.Shared._RMC14.Light;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Synth;
using Content.Shared._RMC14.Thunderdome;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._SpaceFront.Rules;
using Robust.Shared.Audio;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private bool IsSquadWarTeamA(CMDistressSignalRuleComponent distress, EntityUid squad)
    {
        foreach (var id in distress.SquadWarShipTwoSquads)
        {
            if (distress.Squads.TryGetValue(id, out var squadEnt) && squadEnt == squad)
                return true;
        }

        return false;
    }

    private void CheckSquadWarState(CMDistressSignalRuleComponent distress)
    {
        // Частота вызова задаётся ванильным гейтом ActiveTick (component.CheckEvery);
        // внутренний гейт не нужен — он конфликтовал с ванильным NextCheck.
        var now = Timing.CurTime;

        Log.Info($"SquadWar diag: state check — ended={distress.SquadWarEnded}, cur={now}, xenoMap={distress.XenoMap}, ship1={distress.ShipOneGrid}, ship2={distress.ShipTwoGrid}, fogCleared={distress.SquadWarFogCleared}");

        if (distress.SquadWarEnded)
            return;

        // Грид планеты может быть пересоздан (SetUpStation удаляет грид-чертёж
        // и создаёт новый) — тогда XenoMap указывает на удалённую сущность.
        // Переищем настоящий грид по сохранённому MapId карты планеты.
        if (distress.XenoMap is { } oldGrid &&
            (!TryComp<TransformComponent>(oldGrid, out _) || TerminatingOrDeleted(oldGrid)))
        {
            distress.XenoMap = null;
        }

        if (distress.XenoMap == null && distress.SquadWarPlanetMapId is { } planetMapId)
        {
            var planetGridQuery = EntityQueryEnumerator<MapGridComponent, TransformComponent>();
            while (planetGridQuery.MoveNext(out var pgUid, out _, out var pgXform))
            {
                if (pgXform.MapID == planetMapId)
                {
                    distress.XenoMap = pgUid;
                    Log.Info($"SquadWar diag: xeno grid re-linked to {pgUid} (map {planetMapId})");
                    break;
                }
            }
        }

        // Туман строится как только грид планеты доступен (и только один раз).
        if (!distress.SquadWarFogDone && distress.XenoMap != null)
            SpawnSquadWarFog(distress);

        if (distress.XenoMap is not { } planetGrid ||
            distress.ShipOneGrid is not { } shipOne ||
            distress.ShipTwoGrid is not { } shipTwo)
        {
            return;
        }

        // Space Front: туман рассеивается, как только ОБА шаттла на планете
        // (независимо от наличия на них живых игроков).
        if (!distress.SquadWarFogCleared && TryComp<TransformComponent>(distress.XenoMap, out var planetXform))
        {
            var planetMap = planetXform.MapUid;
            var totalShuttles = 0;
            var shuttlesOnPlanet = 0;
            var shuttleQuery = EntityQueryEnumerator<DropshipComponent, TransformComponent>();
            while (shuttleQuery.MoveNext(out _, out _, out var shuttleXform))
            {
                totalShuttles++;
                if (shuttleXform.MapUid == planetMap)
                    shuttlesOnPlanet++;
            }

            Log.Info($"SF diag: fog check — totalShuttles={totalShuttles}, onPlanet={shuttlesOnPlanet}, planetMap={planetMap}");

            if (totalShuttles >= 2 && shuttlesOnPlanet >= 2)
            {
                Log.Info($"SquadWar: туман рассеян — оба шаттла на планете ({shuttlesOnPlanet}/{totalShuttles})");
                ClearSquadWarFog(distress);
            }
        }

        var teamAAlivePlanet = 0;
        var teamADeadPlanet = 0;
        var teamBAlivePlanet = 0;
        var teamBDeadPlanet = 0;
        var teamAAliveTotal = 0;
        var teamBAliveTotal = 0;

        // Карта планеты: считаем игрока «на планете», даже если он внутри шаттла,
        // стоящего на планете (грид шаттла != гриду планеты, но карта одна и та же).
        var planetMapUid = Transform(planetGrid).MapUid;

        // Считаем по меткам команд SquadWar (они есть у ВСЕХ игроков режима),
        // а не по отрядам: у мобов SquadWar может не быть SquadMemberComponent.
        var query = EntityQueryEnumerator<SquadWarTeamComponent, MobStateComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var teamComp, out var mobState, out var xform))
        {
            // АФК/ССД не учитываются нигде.
            if (!IsActivePlayer(uid))
                continue;

            var alive = _mobState.IsAlive(uid, mobState);
            var onPlanet = xform.MapUid == planetMapUid;

            if (teamComp.Team == "АБ")
            {
                if (alive)
                {
                    teamAAliveTotal++;
                    if (onPlanet)
                        teamAAlivePlanet++;
                }
                else if (onPlanet)
                {
                    teamADeadPlanet++;
                }
            }
            else
            {
                if (alive)
                {
                    teamBAliveTotal++;
                    if (onPlanet)
                        teamBAlivePlanet++;
                }
                else if (onPlanet)
                {
                    teamBDeadPlanet++;
                }
            }
        }

        // Диагностика захвата — в лог каждую проверку.
        Log.Info($"SquadWar diag: control check — AAlivePlanet={teamAAlivePlanet}, BAlivePlanet={teamBAlivePlanet}, ATotal={teamAAliveTotal}, BTotal={teamBAliveTotal}, fogCleared={distress.SquadWarFogCleared}, controlling={distress.SquadWarControllingTeam}, start={distress.SquadWarPlanetControlStart}, unlocked1={distress.SquadWarShipOneUnlocked}, unlocked2={distress.SquadWarShipTwoUnlocked}");

        // Контроль планеты: туман рассеялся + 1 минута непрерывного контроля без живых бойцов врага.
        if (!distress.SquadWarFogCleared)
        {
            distress.SquadWarPlanetControlStart = null;
            distress.SquadWarControllingTeam = 0;
        }
        else
        {
            var controlling = 0;
            if (teamAAlivePlanet > 0 && teamBAlivePlanet == 0)
                controlling = 1; // АБ
            else if (teamBAlivePlanet > 0 && teamAAlivePlanet == 0)
                controlling = 2; // ДЧ

            if (controlling == 0)
            {
                distress.SquadWarPlanetControlStart = null;
                distress.SquadWarControllingTeam = 0;
            }
            else if (distress.SquadWarControllingTeam == controlling)
            {
                var controlStart = distress.SquadWarPlanetControlStart ?? Timing.CurTime;
                distress.SquadWarPlanetControlStart = controlStart;

                if (Timing.CurTime - controlStart >= distress.SquadWarPlanetControlTime)
                {
                    if (controlling == 1 && !distress.SquadWarShipTwoUnlocked)
                    {
                        distress.SquadWarShipTwoUnlocked = true;
                        _chatManager.DispatchServerAnnouncement(Loc.GetString("rmc-squad-war-planet-captured-ab"));
                    }
                    else if (controlling == 2 && !distress.SquadWarShipOneUnlocked)
                    {
                        distress.SquadWarShipOneUnlocked = true;
                        _chatManager.DispatchServerAnnouncement(Loc.GetString("rmc-squad-war-planet-captured-cd"));
                    }

                    // Сброс — другая команда может отбить планету и получить свой порт.
                    distress.SquadWarPlanetControlStart = null;
                    distress.SquadWarControllingTeam = 0;
                }
            }
            else
            {
                distress.SquadWarPlanetControlStart = Timing.CurTime;
                distress.SquadWarControllingTeam = controlling;
            }
        }
    }

    /// <summary>
    /// SquadWar: фиксирует прибытие шаттла на вражеский Алмаер (обычный FTL).
    /// </summary>
    /// <summary>
    /// SquadWar: если на шаттле есть игроки вражеской команды и шаттл летит на Алмаер —
    /// объявляем красный код и тревогу (штурм корабля).
    /// </summary>

    public void CheckSquadWarEnemyFlight(Entity<DropshipComponent> ent)
    {
        var rule = TryGetActiveRuleNewest();
        if (rule is not { SquadWar: true })
            return;

        // Летит ли шаттл на корабль?
        if (ent.Comp.Destination is not { } dest)
        {
            Log.Info($"SquadWar diag: enemy flight — no destination");
            return;
        }

        var destGrid = Transform(dest).GridUid;
        if (destGrid != rule.ShipOneGrid && destGrid != rule.ShipTwoGrid)
        {
            Log.Info($"SquadWar diag: enemy flight — dest grid {destGrid} is not a ship (ships: {rule.ShipOneGrid}, {rule.ShipTwoGrid})");
            return;
        }

        // Тревога объявляется на каждом корабле один раз за раунд.
        var defendingShipTwo = destGrid == rule.ShipTwoGrid;
        if (defendingShipTwo ? rule.SquadWarShipTwoInvasionAlerted : rule.SquadWarShipOneInvasionAlerted)
            return;

        // Корабль-цель: на корабль АБ летят ДЧ, на корабль ДЧ — АБ.
        var attackers = defendingShipTwo ? "ДЧ" : "АБ";

        // На борту есть активные игроки атакующей команды?
        // Ищем по ГРИДУ шаттла (игрок может сидеть в кресле — тогда он не прямой ребёнок грида).
        var shuttleGridUid = Transform(ent.Owner).GridUid;
        var enemies = false;
        var aboardQuery = EntityQueryEnumerator<SquadWarTeamComponent, TransformComponent>();
        while (aboardQuery.MoveNext(out var child, out var team, out var xform))
        {
            if (xform.GridUid != shuttleGridUid)
                continue;

            Log.Info($"SquadWar diag: alert aboard — uid={child}, team={team.Team}, active={IsActivePlayer(child)}");

            if (team.Team == attackers && IsActivePlayer(child))
            {
                enemies = true;
                break;
            }
        }

        if (!enemies)
        {
            Log.Info($"SquadWar diag: enemy flight — no attackers aboard, attackers={attackers}, ship2={defendingShipTwo}");
            return;
        }

        Log.Info($"SquadWar diag: enemy flight — ALERT, attackers={attackers}, ship2={defendingShipTwo}");

        if (defendingShipTwo)
            rule.SquadWarShipTwoInvasionAlerted = true;
        else
            rule.SquadWarShipOneInvasionAlerted = true;

        // Красный код — только на корабле-цели, объявление слышат все.
        var shipGrid = defendingShipTwo ? rule.ShipTwoGrid : rule.ShipOneGrid;
        if (shipGrid is { } grid)
            _alertLevelSystem.Set(RMCAlertLevels.Red, ent.Owner, false, false, Transform(grid).MapUid);

        _marineAnnounce.AnnounceARESStaging(null,
            Loc.GetString(attackers == "АБ" ? "rmc-squad-war-ab-attacking" : "rmc-squad-war-dch-attacking"));

        // Боевая тревога (General Quarters) с сиреной — как при ванильном вторжении ксеносов.
        var generalQuartersText = Loc.GetString("rmc-announcement-general-quarters");
        Timer.Spawn(TimeSpan.FromSeconds(10), () =>
        {
            if (Deleted(ent.Owner))
                return;

            _marineAnnounce.AnnounceARESStaging(ent.Owner, generalQuartersText, ent.Comp.GeneralQuartersSound, null);
        });
    }

    /// <summary>
    /// SquadWar: самое новое активное правило (старые станции прошлых раундов
    /// могут не удаляться — тогда TryGetActiveRule может вернуть устаревшее правило
    /// с уже взведёнными флагами скоттла/эвакуации).
    /// </summary>

    public CMDistressSignalRuleComponent? TryGetActiveRuleNewest()
    {
        CMDistressSignalRuleComponent? newest = null;
        var query = EntityQueryEnumerator<CMDistressSignalRuleComponent>();
        while (query.MoveNext(out var uid, out var rule))
        {
            if (!rule.SquadWar || rule.SquadWarEnded)
                continue;

            if (newest == null || uid.Id > newest.Owner.Id)
                newest = rule;
        }

        return newest;
    }

    /// <summary>
    /// SquadWar: принудительно рассеивает туман (тестовая команда).
    /// </summary>

    public void ClearSquadWarFog(CMDistressSignalRuleComponent rule)
    {
        if (rule.SquadWarFogCleared)
            return;

        rule.SquadWarFogCleared = true;
        var query = EntityQueryEnumerator<MetaDataComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var meta, out _))
        {
            if (meta.EntityPrototype?.ID == "RMCFogWall30")
                Del(uid);
        }

        // Полный сценарий рассеивания: анонс ARES и музыка, как при естественном таймере.
        _marineAnnounce.AnnounceARESStaging(null,
            Loc.GetString("rmc-squad-war-fog-cleared"),
            new SoundPathSpecifier("/Audio/_SpaceFront/Music/fog_clear.ogg"));

        Log.Info("SquadWar: туман рассеян командой clearfog");
    }

    public void OnDropshipFTLCompletedSquadWar(Entity<DropshipComponent> ent, EntityUid map)
    {
        var rule = TryGetActiveRuleNewest();
        if (rule is not { SquadWar: true })
        {
            Log.Info("SquadWar diag: arrival — not SquadWar rule");
            return;
        }

        // Пролёт (Flyby) — не штурм: шаттл не садится на корабль.
        if (HasComp<DropshipInFlyByComponent>(ent))
        {
            Log.Info("SquadWar diag: arrival — flyby, not an assault");
            return;
        }

        // Space Front: шаттл прибыл на ПЛАНЕТУ — отмечаем высадку команды.
        // Когда высадились ОБЕ команды — туман рассеивается сразу.
        if (rule.XenoMap is { } xenoGrid && Transform(xenoGrid).MapUid == map)
        {
            var shuttleGrid = Transform(ent).GridUid;
            var isAB = shuttleGrid != null && HasComp<SquadWarShipTwoComponent>(shuttleGrid.Value);
            if (isAB)
                rule.SquadWarPlanetArrivedAB = true;
            else
                rule.SquadWarPlanetArrivedDCH = true;

            Log.Info($"SquadWar diag: planet arrival — АБ={rule.SquadWarPlanetArrivedAB}, ДЧ={rule.SquadWarPlanetArrivedDCH}");

            if (rule.SquadWarPlanetArrivedAB && rule.SquadWarPlanetArrivedDCH)
            {
                Log.Info("SquadWar: туман рассеян — обе команды высадились на планету");
                ClearSquadWarFog(rule);
            }

            return;
        }

        if (rule.SquadWarArrivedTeam != 0)
        {
            Log.Info($"SquadWar diag: arrival — already arrived (team {rule.SquadWarArrivedTeam})");
            return;
        }

        if (rule.ShipOneGrid is not { } shipOne || rule.ShipTwoGrid is not { } shipTwo)
        {
            Log.Info($"SquadWar diag: arrival — missing ships ({rule.ShipOneGrid}, {rule.ShipTwoGrid})");
            return;
        }

        var shipOneMap = Transform(shipOne).MapUid;
        var shipTwoMap = Transform(shipTwo).MapUid;
        Log.Info($"SquadWar diag: arrival — map={map}, shipOneMap={shipOneMap}, shipTwoMap={shipTwoMap}");

        // Чей корабль — цель высадки? 1 — АБ (корабль 2), 2 — ДЧ (корабль 1).
        var destinationTeam = map == shipOneMap ? 2 : map == shipTwoMap ? 1 : 0;
        if (destinationTeam == 0)
        {
            Log.Info("SquadWar diag: arrival — destination is not an enemy ship");
            return;
        }


        // На борту должен быть активный игрок ПРОТИВОПОЛОЖНОЙ команды (шаттл могли угнать).
        // Ищем по ГРИДУ шаттла: игрок может сидеть в кресле (тогда он не прямой ребёнок грида).
        var hasEnemy = false;
        var shuttleGridUid = Transform(ent).GridUid;
        var aboardCount = 0;
        var aboardQuery = EntityQueryEnumerator<SquadWarTeamComponent, TransformComponent>();
        while (aboardQuery.MoveNext(out var child, out var team, out var xform))
        {
            if (xform.GridUid != shuttleGridUid)
                continue;

            aboardCount++;
            Log.Info($"SquadWar diag: arrival — aboard uid={child}, team={team.Team}, active={IsActivePlayer(child)}");

            if (!IsActivePlayer(child))
                continue;

            var teamNum = team.Team == "АБ" ? 1 : 2;
            if (teamNum != destinationTeam)
            {
                hasEnemy = true;
                break;
            }
        }

        if (!hasEnemy)
        {
            // Space Front: СВОЙ шаттл на СВОЁМ корабле без врагов на борту — не штурм
            // (чтобы вражеский игрок, уже находящийся у ангара, не считался атакующим).
            // Но если враги БЫЛИ на борту (угон) — штурм уже засчитан выше.
            var shuttleGridNow = Transform(ent).GridUid;
            var isABShuttleNow = shuttleGridNow != null && HasComp<SquadWarShipTwoComponent>(shuttleGridNow.Value);
            var shuttleTeamNow = isABShuttleNow ? 1 : 2;
            if (shuttleTeamNow == destinationTeam)
            {
                Log.Info($"SquadWar diag: arrival — own shuttle at own ship without attackers aboard (destTeam={destinationTeam}), not an assault");
                return;
            }

            // Фолбэк 1: атакующие могли уже выйти из шаттла — ищем их рядом (радиус 6).
            var shuttlePos = Transform(ent).WorldPosition;
            var nearQuery = EntityQueryEnumerator<SquadWarTeamComponent, TransformComponent>();
            while (nearQuery.MoveNext(out var nearUid, out var nearTeam, out var nearXform))
            {
                var teamNumNear = nearTeam.Team == "АБ" ? 1 : 2;
                if (teamNumNear == destinationTeam)
                    continue;

                if (!IsActivePlayer(nearUid))
                    continue;

                if ((nearXform.WorldPosition - shuttlePos).Length() <= 6f)
                {
                    hasEnemy = true;
                    Log.Info($"SquadWar diag: arrival — attacker found near shuttle (uid={nearUid}, team={nearTeam.Team})");
                    break;
                }
            }
        }

        if (!hasEnemy)
        {
            Log.Info($"SquadWar diag: arrival — no attackers aboard (checked {aboardCount}, destTeam={destinationTeam})");
            return;
        }

        // Диагностика: почему блок может пропускаться.
        Log.Info($"SF diag: assault — rule={rule.Owner}, unlocked={rule.ScuttleUnlocked}, unlockAt={rule.ScuttleUnlockAt}, detonated={rule.ScuttleDetonated}, evacInProgress={_evacuation.IsEvacuationInProgress()}");

        // Разблокировка подрыва реакторов — как при ванильной высадке ксеносов на Алмаер.
        if (!rule.ScuttleUnlocked && rule.ScuttleUnlockAt == null && !rule.ScuttleDetonated)
        {
            rule.ScuttleUnlockAt = Timing.CurTime + rule.ScuttleUnlockDelay;
            Log.Info("SquadWar: подрыв реакторов разблокирован");
        }

        // Автоматическая эвакуация на корабле-цели: насосы начинают качать топливо,
        // двери эвакуационных шлюпок открываются.
        var targetShipGrid = destinationTeam == 1 ? rule.ShipTwoGrid : rule.ShipOneGrid;
        rule.SquadWarScuttleTargetGrid = targetShipGrid;
        if (targetShipGrid is { } targetGrid)
        {
            var targetMap = Transform(targetGrid).MapUid;
            if (!_evacuation.IsEvacuationInProgress())
            {
                _evacuation.ToggleEvacuation(null, null, targetMap);
                Log.Info($"SquadWar: эвакуация включена на корабле-цели (map {targetMap})");
            }

            // Ванильное условие работы насосов: «шаттл разбился об Алмаер».
            // В нашем режиме шаттл садится штатно, поэтому помечаем вручную
            // (метод внутри системы эвакуации, где есть доступ к полям).
            _evacuation.ForceEvacuationCrashState(targetMap.GetValueOrDefault());
        }

        rule.SquadWarArrivedTeam = destinationTeam == 1 ? 2 : 1;
        Log.Info($"SquadWar: высадка на вражеский Алмаер — команда {(rule.SquadWarArrivedTeam == 1 ? "АБ" : "ДЧ")}");
    }

    /// <summary>
    /// SquadWar: назначает метку команды игрокам, у которых её нет.
    /// Корабль АБ → АБ, корабль ДЧ → ДЧ, вне кораблей (планета и т.д.) — случайно.
    /// </summary>

    private void AssignMissingTeamMarks(CMDistressSignalRuleComponent comp)
    {
        // Все гуманоиды без метки (включая заспавненных админом/твинков без сессии).
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (HasComp<SquadWarTeamComponent>(uid) || HasComp<XenoComponent>(uid))
                continue;

            var grid = xform.GridUid;
            string team;
            if (grid == comp.ShipTwoGrid)
                team = "АБ";
            else if (grid == comp.ShipOneGrid)
                team = "ДЧ";
            else
                team = _random.Next(2) == 0 ? "АБ" : "ДЧ";

            var teamComp = EnsureComp<SquadWarTeamComponent>(uid);
            teamComp.Team = team;
            Dirty(uid, teamComp);

            if (team == "АБ")
                comp.SquadWarHadABPlayer = true;
            else
                comp.SquadWarHadDCHPlayer = true;

            Log.Info($"SquadWar: назначена команда {team} игроку без отряда (uid={uid}, grid={grid})");
        }

        // Space Front: командные турели — игроки получают NpcFaction-фракцию своей команды.
        var teamQuery = EntityQueryEnumerator<SquadWarTeamComponent>();
        var npcFaction = EntitySystem.Get<NpcFactionSystem>();
        while (teamQuery.MoveNext(out var uid, out var teamComp))
        {
            var faction = teamComp.Team == "АБ" ? "SquadWarAB" : "SquadWarDCH";
            npcFaction.AddFaction(uid, faction);
        }
    }

    /// <summary>
    /// SquadWar: проверяет, уничтожена ли одна из команд полностью, и завершает раунд победой второй.
    /// </summary>

    private void CheckSquadWarElimination(CMDistressSignalRuleComponent comp)
    {
        if (Deleted(comp.Owner) ||
            comp.SquadWarEnded ||
            comp.StartTime == null ||
            Timing.CurTime - comp.StartTime < TimeSpan.FromSeconds(30))
        {
            return;
        }

        // Игроки без метки команды: назначаем по кораблю (или случайно вне кораблей).
        AssignMissingTeamMarks(comp);

        var aliveAB = 0;
        var aliveDCH = 0;
        var teamQuery = EntityQueryEnumerator<SquadWarTeamComponent, MobStateComponent>();
        while (teamQuery.MoveNext(out var teamUid, out var team, out var mobState))
        {
            if (mobState.CurrentState == MobState.Dead)
                continue;

            // АФК/ССД (нет активной сессии) не считаются.
            if (!IsActivePlayer(teamUid))
                continue;

            if (team.Team == "АБ")
                aliveAB++;
            else if (team.Team == "ДЧ")
                aliveDCH++;
        }

        // НИЧЬЯ: в обеих командах не осталось живых активных игроков (все мертвы или АФК/ССД).
        // Ждём 5 минут — если за это время никто не ожил и не вернулся, объявляем ничью.
        // Таймер сбрасывается, как только хотя бы один игрок снова активен и жив.
        if ((comp.SquadWarHadABPlayer || comp.SquadWarHadDCHPlayer) && aliveAB == 0 && aliveDCH == 0)
        {
            comp.SquadWarDrawStart ??= Timing.CurTime;
            if (Timing.CurTime - comp.SquadWarDrawStart >= TimeSpan.FromMinutes(1))
            {
                EndSquadWarByDraw(comp);
                return;
            }
        }
        else
        {
            comp.SquadWarDrawStart = null;
        }

        // Команда считается уничтоженной, только если в ней 1 минуту подряд нет живых активных игроков.
        var eliminationCandidate = 0;
        if (comp.SquadWarHadABPlayer && aliveAB == 0 && comp.SquadWarHadDCHPlayer && aliveDCH > 0)
            eliminationCandidate = 2; // уничтожена АБ
        else if (comp.SquadWarHadDCHPlayer && aliveDCH == 0 && comp.SquadWarHadABPlayer && aliveAB > 0)
            eliminationCandidate = 1; // уничтожена ДЧ

        if (eliminationCandidate != 0 && comp.SquadWarEliminatedTeam == 0)
        {
            if (comp.SquadWarEliminationHoldTeam != eliminationCandidate)
            {
                comp.SquadWarEliminationHoldTeam = eliminationCandidate;
                comp.SquadWarEliminationHoldStart = Timing.CurTime;
            }
            else if (Timing.CurTime - comp.SquadWarEliminationHoldStart >= TimeSpan.FromMinutes(1))
            {
                comp.SquadWarEliminatedTeam = eliminationCandidate;
                if (eliminationCandidate == 2)
                    _marineAnnounce.AnnounceARESStaging(null, Loc.GetString("rmc-squad-war-enemy-eliminated-ab"));
                else
                    _marineAnnounce.AnnounceARESStaging(null, Loc.GetString("rmc-squad-war-enemy-eliminated-dch"));
            }
        }
        else
        {
            comp.SquadWarEliminationHoldTeam = 0;
            comp.SquadWarEliminationHoldStart = null;
        }

        // Если корабль уже взрывается — решает ничья, победы команд больше не засчитываются.
        if (comp.ScuttleDetonated || comp.ScuttleFinalSequenceStarted)
            return;

        // Победа: враг РЕАЛЬНО уничтожен И победители высадились на вражеский Алмаер.
        // Пустая команда (никто не зашёл) уничтожением не считается — раунд с одним игроком не завершается.
        // Если кто-то вышел из крио после объявления «потерь» — уничтожение уже недействительно:
        // победа требует, чтобы враг был мёртв прямо сейчас (пересчёт живых выше).
        var dchEliminated = comp.SquadWarEliminatedTeam == 1 && aliveDCH == 0;
        var abEliminated = comp.SquadWarEliminatedTeam == 2 && aliveAB == 0;

        // Сброс статуса «уничтожена», если команда ожила (крио) — объявление больше не актуально.
        if (comp.SquadWarEliminatedTeam == 1 && aliveDCH > 0)
        {
            comp.SquadWarEliminatedTeam = 0;
            Log.Info("SquadWar diag: elimination reset — ДЧ players are alive again");
        }
        else if (comp.SquadWarEliminatedTeam == 2 && aliveAB > 0)
        {
            comp.SquadWarEliminatedTeam = 0;
            Log.Info("SquadWar diag: elimination reset — АБ players are alive again");
        }

        var victoryAB = comp.SquadWarArrivedTeam == 1 && dchEliminated;
        var victoryDCH = comp.SquadWarArrivedTeam == 2 && abEliminated;

        // Победа: враг (защитники) успешно эвакуировался со СВОЕГО корабля.
        var victoryEvacAB = comp.SquadWarArrivedTeam == 1 && comp.SquadWarEvacSucceeded && comp.SquadWarEvacShip == 2;
        var victoryEvacDCH = comp.SquadWarArrivedTeam == 2 && comp.SquadWarEvacSucceeded && comp.SquadWarEvacShip == 1;

        // Все концовки засчитываются, только если условия продержались 1 минуту
        // (защита от случайных/мгновенных срабатываний).
        if (victoryAB || victoryDCH || victoryEvacAB || victoryEvacDCH)
        {
            comp.SquadWarVictoryHoldStart ??= Timing.CurTime;
            if (Timing.CurTime - comp.SquadWarVictoryHoldStart >= TimeSpan.FromMinutes(1))
            {
                if (victoryAB)
                    EndSquadWarByElimination(comp, "АБ");
                else if (victoryDCH)
                    EndSquadWarByElimination(comp, "ДЧ");
                else if (victoryEvacAB)
                    EndSquadWarByElimination(comp, "АБ", evac: true);
                else if (victoryEvacDCH)
                    EndSquadWarByElimination(comp, "ДЧ", evac: true);
                return;
            }
        }
        else
        {
            comp.SquadWarVictoryHoldStart = null;
        }
    }

    /// <summary>
    /// SquadWar: победа полным уничтожением противника — объявление и манифест (конец раунда).
    /// </summary>

    private void EndSquadWarByElimination(CMDistressSignalRuleComponent distress, string winnerTeam, bool evac = false)
    {
        if (distress.SquadWarEnded)
            return;

        distress.SquadWarEnded = true;
        distress.Result = DistressSignalRuleResult.MajorMarineVictory;

        var ab = winnerTeam == "АБ";
        distress.CustomRoundEndMessage = evac
            ? ab ? "rmc-squad-war-roundend-evac-ab" : "rmc-squad-war-roundend-evac-dch"
            : ab ? "rmc-squad-war-roundend-ab" : "rmc-squad-war-roundend-dch";

        var ares = _aresCore.EnsureMarineARES();
        _marineAnnounce.AnnounceARESStaging(ares,
            Loc.GetString(evac
                ? ab ? "rmc-squad-war-victory-evac-ab" : "rmc-squad-war-victory-evac-dch"
                : ab ? "rmc-squad-war-victory-ab" : "rmc-squad-war-victory-dch"),
            null, null);

        // Конец раунда с манифестом, как в обычной игре.
        _roundEnd.EndRound();
    }

    /// <summary>
    /// SquadWar: ничья — в обеих командах не осталось живых активных игроков (5 минут без выживших).
    /// </summary>

    private void EndSquadWarByDraw(CMDistressSignalRuleComponent distress)
    {
        if (distress.SquadWarEnded)
            return;

        distress.SquadWarEnded = true;
        distress.Result = DistressSignalRuleResult.AllDied;
        distress.CustomRoundEndMessage = "rmc-squad-war-roundend-draw";

        var ares = _aresCore.EnsureMarineARES();
        _marineAnnounce.AnnounceARESStaging(ares,
            Loc.GetString("rmc-squad-war-draw-all-died"),
            null, null);

        _roundEnd.EndRound();
    }

    private void EndSquadWarRound(CMDistressSignalRuleComponent distress)
    {
        if (distress.SquadWarEnded)
            return;

        distress.SquadWarEnded = true;
        distress.Result = DistressSignalRuleResult.MajorMarineVictory;
        _roundEnd.EndRound();
    }

    /// <summary>
    /// Ends the round with the specified result, handling victory conditions and announcements.
    /// </summary>
}
