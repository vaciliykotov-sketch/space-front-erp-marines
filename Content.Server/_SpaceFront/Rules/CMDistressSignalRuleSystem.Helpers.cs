using Content.Server.Power.Components;
using Content.Server.Spawners.Components;
using Content.Shared.Access.Components;
using Content.Shared.Coordinates;
using Content.Shared.Doors.Components;
using Content.Shared.Fax.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Roles;
using Content.Shared._RMC14.Item;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Thunderdome;
using Content.Shared._RMC14.WeedKiller;
using Content.Shared._RMC14.Xenonids.Construction.FloorResin;
using Content.Shared._RMC14.Xenonids.Construction.Tunnel;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._SpaceFront.Rules;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private void OnSquadWarAccessInit(Entity<AccessReaderComponent> ent, ref MapInitEvent args)
    {
        if (TryGetActiveRule() is not { SquadWar: true } || HasComp<DoorComponent>(ent))
            return;

        RemCompDeferred<AccessReaderComponent>(ent);
    }

    /// <summary>
    /// SquadWar: интерфейсы консолей открыты всем независимо от доступов.
    /// </summary>

    private void OnSquadWarUIAccessInit(Entity<ActivatableUIRequiresAccessComponent> ent, ref MapInitEvent args)
    {
        if (TryGetActiveRule() is not { SquadWar: true })
            return;

        RemCompDeferred<ActivatableUIRequiresAccessComponent>(ent);
    }

    /// <summary>
    /// SquadWar: игрок активен — есть сессия, привязанная к его мобу (не АФК/ССД).
    /// </summary>

    private bool IsActivePlayer(EntityUid mob)
    {
        // Тестовые марионетки всегда считаются активными.
        if (HasComp<SquadWarTestMarionetteComponent>(mob))
            return true;

        return _player.TryGetSessionByEntity(mob, out var session) &&
               session.AttachedEntity == mob;
    }

    /// <summary>
    /// SquadWar: активные живые члены отряда (без мёртвых и АФК).
    /// </summary>

    private int CountActiveSquadMembers(Entity<SquadTeamComponent> squad)
    {
        var count = 0;
        foreach (var member in squad.Comp.Members)
        {
            if (Deleted(member))
                continue;

            if (TryComp(member, out MobStateComponent? mobState) &&
                mobState.CurrentState == MobState.Dead)
                continue;

            if (!IsActivePlayer(member))
                continue;

            count++;
        }

        return count;
    }

    /// <summary>
    /// SquadWar: роли, которых может быть не больше одной на каждом корабле.
    /// </summary>

    private static readonly HashSet<string> LimitedPerShipRoles = new()
    {
        "CMCommandingOfficer",
        "CMExecutiveOfficer",
        "CMQuartermaster",
        "CMChiefMP",
        "CMCMO",
        "CMChiefEngineer",
    };

    /// <summary>
    /// SquadWar: считает всех игроков на корабле (отряды корабля + пятый отряд).
    /// </summary>

    private int CountShipPlayers(CMDistressSignalRuleComponent rule, EntityUid? shipGrid)
    {
        var total = 0;
        if (shipGrid == null)
            return total;

        if (shipGrid == rule.ShipTwoGrid)
            total += rule.ShipTwoNonSquad;
        else if (shipGrid == rule.ShipOneGrid)
            total += rule.ShipOneNonSquad;

        foreach (var (squadId, squad) in rule.Squads)
        {
            var onShipTwo = rule.SquadWarShipTwoSquads.Contains(squadId);
            var isThisShip = onShipTwo
                ? shipGrid == rule.ShipTwoGrid
                : shipGrid == rule.ShipOneGrid;

            if (!isThisShip || !TryComp(squad, out SquadTeamComponent? team))
                continue;

            total += CountActiveSquadMembers(new Entity<SquadTeamComponent>(squad, team));
        }

        return total;
    }

    /// <summary>
    /// SquadWar: оставляет только спавнеры на указанной сетке (с фолбэком на все).
    /// </summary>

    private List<EntityUid> FilterSpawnersByGrid(List<EntityUid> spawners, EntityUid? grid)
    {
        if (grid == null)
            return spawners;

        var result = new List<EntityUid>();
        foreach (var spawner in spawners)
        {
            if (Transform(spawner).GridUid == grid)
                result.Add(spawner);
        }

        return result.Count > 0 ? result : spawners;
    }

    private List<EntityUid> FilterSquadWarSpawners(CMDistressSignalRuleComponent rule, List<EntityUid> spawners, EntProtoId? squadId)
    {
        if (!rule.SquadWar)
            return spawners;

        var grid = squadId is { } sid && rule.SquadWarShipTwoSquads.Contains(sid)
            ? rule.ShipTwoGrid
            : rule.ShipOneGrid;

        if (grid == null)
            return spawners;

        var result = new List<EntityUid>();
        foreach (var spawner in spawners)
        {
            if (Transform(spawner).GridUid == grid)
                result.Add(spawner);
        }

        return result.Count > 0 ? result : spawners;
    }

    /// <summary>
    /// Finds the best spawn point for a player based on their job, squad preference, and availability.
    /// Falls back to generic spawn points if preferred ones are unavailable.
    /// </summary>
}
