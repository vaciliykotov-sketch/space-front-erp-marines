using System.Numerics;
using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared.Bed.Sleep;
using Content.Shared.Drowsiness;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Map;
using Robust.Shared.GameObjects;
using Robust.Shared.Console;
using Content.Shared._SpaceFront.Rules;

namespace Content.Server._SpaceFront.MapEditor;

/// <summary>
/// Тестовые марионетки SquadWar. Подкоманды:
///   swtest spawn ab|dch|both [count] — твинки одной команды или обеих;
///   swtest clearfog — рассеять туман;
///   swtest kill АБ|ДЧ — убить команду;
///   swtest planet АБ|ДЧ — показать планету.
/// </summary>
[AdminCommand(AdminFlags.Spawn)]
public sealed class SquadWarTestConsoleCommand : IConsoleCommand
{
    public string Command => "swtest";
    public string Description => "SquadWar: тестовые марионетки, туман, убийство команды.";
    public string Help => "swtest spawn <ab|dch|both> [count] | clearfog | kill <АБ|ДЧ> | planet <АБ|ДЧ>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteLine(Help);
            return;
        }

        var entMan = IoCManager.Resolve<IEntityManager>();
        var ruleSys = entMan.EntitySysManager.GetEntitySystem<CMDistressSignalRuleSystem>();

        if (args[0] == "clear")
        {
            // Удаляет всех мобов без активной сессии (тестовые Уристы, марионетки) —
            // чтобы очистить PVS и вернуть производительность.
            var count = 0;
            var query = entMan.EntityQueryEnumerator<MobStateComponent>();
            while (query.MoveNext(out var uid, out _))
            {
                if (entMan.HasComponent<Robust.Shared.Player.ActorComponent>(uid))
                    continue;

                entMan.DeleteEntity(uid);
                count++;
            }

            shell.WriteLine($"SquadWar: удалено мобов без сессии: {count}");
            return;
        }
        var rule = ruleSys.ActiveRule;
        if (rule == null)
        {
            shell.WriteLine("Нет активного правила SquadWar.");
            return;
        }

        switch (args[0].ToLowerInvariant())
        {
            case "clearfog":
                rule.SquadWarFogCleared = true;
                ruleSys.ClearSquadWarFog(rule);
                shell.WriteLine("Туман рассеян.");
                return;

            case "spawn":
                SpawnTwinks(shell, entMan, rule, args);
                return;

            case "kill":
                KillTeam(shell, entMan, args);
                return;

            case "planet":
                GoPlanet(shell, entMan, rule, args);
                return;

            default:
                shell.WriteLine(Help);
                return;
        }
    }

    private void SpawnTwinks(IConsoleShell shell, IEntityManager entMan, CMDistressSignalRuleComponent rule, string[] args)
    {
        var actor = shell.Player?.AttachedEntity;
        if (actor == null)
        {
            shell.WriteLine("Нет прикреплённой сущности.");
            return;
        }

        var teamFilter = args.Length > 1 ? args[1].ToLowerInvariant() : "both";
        var count = args.Length > 2 && int.TryParse(args[2], out var c) ? c : 10;

        var xformSys = entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        var spawnCoords = xformSys.GetMoverCoordinates(actor.Value);
        var squadSys = entMan.EntitySysManager.GetEntitySystem<SquadSystem>();

        for (var i = 0; i < count; i++)
        {
            var team = teamFilter switch
            {
                "ab" => "АБ",
                "dch" or "cd" or "дч" => "ДЧ",
                _ => i % 2 == 0 ? "АБ" : "ДЧ",
            };

            var coords = spawnCoords.Offset(new Vector2(i % 5 * 0.5f, i / 5 * 0.5f));
            var mob = entMan.SpawnEntity("CMMobHuman", coords);

            entMan.EnsureComponent<SquadWarTestMarionetteComponent>(mob);
            entMan.RemoveComponent<ForcedSleepingStatusEffectComponent>(mob);
            entMan.RemoveComponent<DrowsinessStatusEffectComponent>(mob);

            if (team == "АБ")
                rule.SquadWarHadABPlayer = true;
            else
                rule.SquadWarHadDCHPlayer = true;

            var teamComp = entMan.EnsureComponent<SquadWarTeamComponent>(mob);
            teamComp.Team = team;
            entMan.Dirty(mob, teamComp);

            var squads = team == "АБ" ? rule.SquadWarShipTwoSquads : rule.SquadWarShipOneSquads;
            foreach (var squadId in squads)
            {
                if (rule.Squads.TryGetValue(squadId, out var squadEnt))
                {
                    squadSys.AssignSquad(mob, squadEnt, null);
                    break;
                }
            }
        }

        shell.WriteLine($"Создано {count} тестовых морпехов ({teamFilter}).");
    }

    private void KillTeam(IConsoleShell shell, IEntityManager entMan, string[] args)
    {
        var team = args.Length > 1 ? args[1] : "";
        var mobStateSys = entMan.EntitySysManager.GetEntitySystem<MobStateSystem>();
        var killed = 0;
        var query = entMan.EntityQueryEnumerator<SquadWarTeamComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var teamComp, out var mobState))
        {
            if (mobState.CurrentState == MobState.Dead)
                continue;

            if (teamComp.Team == team)
            {
                mobStateSys.ChangeMobState(uid, MobState.Dead);
                killed++;
            }
        }

        shell.WriteLine($"Убито: {killed}.");
    }

    private void GoPlanet(IConsoleShell shell, IEntityManager entMan, CMDistressSignalRuleComponent rule, string[] args)
    {
        var team = args.Length > 1 ? args[1] : "";
        if (rule.XenoMap is not { } planetMap)
        {
            shell.WriteLine("Планета не найдена.");
            return;
        }

        var xformSys = entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
        var mapId = xformSys.GetMapId(planetMap);
        var moved = 0;
        var query = entMan.EntityQueryEnumerator<SquadWarTeamComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var teamComp, out var xform))
        {
            if (team != "" && teamComp.Team != team)
                continue;

            var destCoords = new EntityCoordinates(planetMap, Vector2.Zero);
            xformSys.SetCoordinates(uid, xform, destCoords);
            moved++;
        }

        shell.WriteLine($"Перемещено на планету: {moved}.");
    }
}
