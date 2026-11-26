using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Chat;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Clothing;
using Content.Shared.Dataset;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Content.Shared.Radio.Components;
using Content.Shared.Radio.EntitySystems;
using Content.Shared.Radio;
using Content.Shared.Roles.Jobs;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Content.Shared.Whitelist;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Commendations;
using Content.Shared._RMC14.Cryostorage;
using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.Orders;
using Content.Shared._RMC14.Pointing;
using Content.Shared._RMC14.Recommendation;
using Content.Shared._RMC14.Roles;
using Content.Shared._RMC14.Tracker;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Content.Shared._RMC14.Marines.Squads;

public sealed partial class SquadSystem : EntitySystem
{
    public int GetSquadTotalMembers(Entity<SquadTeamComponent> team)
    {
        var total = 0;
        foreach (var (_, count) in team.Comp.Roles)
        {
            total += count;
        }

        return total;
    }

    public int GetSquadRoleCount(Entity<SquadTeamComponent> team, ProtoId<JobPrototype> job)
    {
        return team.Comp.Roles.TryGetValue(job, out var count) ? count : 0;
    }

    public bool IsSquadRoleFull(Entity<SquadTeamComponent> team, ProtoId<JobPrototype> job)
    {
        return team.Comp.MaxRoles.TryGetValue(job, out var max) &&
               team.Comp.Roles.TryGetValue(job, out var count) &&
               count >= max;
    }
}
