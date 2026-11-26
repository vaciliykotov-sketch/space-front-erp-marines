using Robust.Shared.Console;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using System.Numerics;
using Content.Shared.Administration;
using Content.Server.Administration;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared.Bed.Sleep;
using Content.Shared.Drowsiness;
using Content.Server._RMC14.Rules.DistressSignal;
using Content.Shared._SpaceFront.Rules;

namespace Content.Server._SpaceFront.MapEditor;

/// <summary>
/// Тестовые марионетки SquadWar: спавн с метками команд, убийство, перемещение на планету.
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Spawn)]
public sealed class SquadWarTestCommand : ToolshedCommand
{
    [CommandImplementation("spawn")]
    public void SpawnSquadWarTest(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] string teamFilter = "both",
        [CommandArgument] int count = 20)
    {
        var actor = ctx.Session?.AttachedEntity;
        if (actor == null)
        {
            ctx.WriteLine("Нет прикреплённой сущности.");
            return;
        }

        var ruleSys = GetSys<CMDistressSignalRuleSystem>();
        var rule = ruleSys.ActiveRule;
        if (rule == null)
        {
            ctx.WriteLine("Нет активного правила.");
            return;
        }

        var xform = GetSys<SharedTransformSystem>();
        var spawnCoords = xform.GetMoverCoordinates(actor.Value);
        var entMan = IoCManager.Resolve<IEntityManager>();

        for (var i = 0; i < count; i++)
        {
            var team = teamFilter.ToLowerInvariant() switch
            {
                "ab" => "АБ",
                "dch" => "ДЧ",
                _ => i % 2 == 0 ? "АБ" : "ДЧ",
            };
            var coords = spawnCoords.Offset(new Vector2(i % 5 * 0.5f, i / 5 * 0.5f));
            var mob = Spawn("CMMobHuman", coords);

            // Маркер тестовой марионетки: считается активным игроком в подсчётах и не спит.
            EnsureComp<SquadWarTestMarionetteComponent>(mob);
            entMan.RemoveComponent<ForcedSleepingStatusEffectComponent>(mob);
            entMan.RemoveComponent<DrowsinessStatusEffectComponent>(mob);

            // Флаги «в команде были игроки» — чтобы условия победы срабатывали.
            if (team == "АБ")
                rule.SquadWarHadABPlayer = true;
            else
                rule.SquadWarHadDCHPlayer = true;

            var teamComp = EnsureComp<SquadWarTeamComponent>(mob);
            teamComp.Team = team;
            entMan.Dirty(mob, teamComp);

            // Отряд, чтобы считался и проверкой контроля планеты.
            var squadSys = GetSys<SquadSystem>();
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

        ctx.WriteLine($"Создано {count} тестовых морпехов (половина АБ, половина ДЧ).");
    }

    [CommandImplementation("clearfog")]
    public void ClearFog(
        [CommandInvocationContext] IInvocationContext ctx)
    {
        var ruleSys = GetSys<CMDistressSignalRuleSystem>();
        var rule = ruleSys.ActiveRule;
        if (rule == null)
        {
            ctx.WriteLine("Нет активного правила.");
            return;
        }

        rule.SquadWarFogCleared = true;
        ruleSys.ClearSquadWarFog(rule);
        ctx.WriteLine("Туман рассеян.");
    }

    [CommandImplementation("kill")]
    public void KillSquadWarTest(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] string team = "AB")
    {
        team = team.ToLowerInvariant() switch
        {
            "ab" => "АБ",
            "dch" => "ДЧ",
            _ => team,
        };
        var mobState = GetSys<MobStateSystem>();
        var entMan = IoCManager.Resolve<IEntityManager>();
        var query = entMan.EntityQueryEnumerator<SquadWarTestMarionetteComponent, SquadWarTeamComponent, MobStateComponent>();
        var killed = 0;
        while (query.MoveNext(out var uid, out _, out var teamComp, out var state))
        {
            if (teamComp.Team != team || state.CurrentState == MobState.Dead)
                continue;

            mobState.ChangeMobState(uid, MobState.Dead);
            killed++;
        }

        ctx.WriteLine($"Убито {killed} тестовых морпехов команды {team}.");
    }

    [CommandImplementation("planet")]
    public void MoveSquadWarTestToPlanet(
        [CommandInvocationContext] IInvocationContext ctx,
        [CommandArgument] string team = "АБ")
    {
        var ruleSys = GetSys<CMDistressSignalRuleSystem>();
        var rule = ruleSys.ActiveRule;
        if (rule?.XenoMap is not { } planetGrid)
        {
            ctx.WriteLine("Планета не найдена.");
            return;
        }

        team = team.ToLowerInvariant() switch
        {
            "ab" => "АБ",
            "dch" => "ДЧ",
            _ => team,
        };
        var xform = GetSys<SharedTransformSystem>();
        var offset = team == "АБ" ? new Vector2(-15, 0) : new Vector2(15, 0);
        var moved = 0;
        var entMan = IoCManager.Resolve<IEntityManager>();
        var query = entMan.EntityQueryEnumerator<SquadWarTestMarionetteComponent, SquadWarTeamComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out _, out var teamComp, out var state))
        {
            if (teamComp.Team != team || state.CurrentState == MobState.Dead)
                continue;

            var tx = Transform(uid);
            var planetCoords = new EntityCoordinates(planetGrid, offset + new Vector2(moved % 5, moved / 5));
            xform.SetCoordinates(uid, tx, planetCoords);
            moved++;
        }

        ctx.WriteLine($"Перемещено на планету {moved} тестовых морпехов команды {team}.");
    }
}
