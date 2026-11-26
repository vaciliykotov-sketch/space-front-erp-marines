using Content.Server.Doors.Systems;
using Content.Server.GameTicking;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server._RMC14.GameStates;
using Content.Server._RMC14.Marines;
using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server._RMC14.Shuttles;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Shuttles.Components;
using Content.Shared.Shuttles.Systems;
using Content.Shared.Timing;
using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Utility.Components;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Announce;
using Content.Shared._RMC14.Xenonids;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Linq;
using System.Numerics;
using Content.Shared._SpaceFront.Rules;

namespace Content.Server._RMC14.Dropship;

public sealed partial class DropshipSystem : SharedDropshipSystem
{
    [Dependency] private readonly CMDistressSignalRuleSystem _distressSignal = default!;

    private bool CanFlyToSquadWar(Entity<DropshipNavigationComputerComponent> computer, EntityUid destination, bool hijack)
    {
        var rule = _distressSignal.ActiveRule;
        if (rule is not { SquadWar: true })
            return true;

        if (hijack)
            return true;

        var shuttleGrid = Transform(computer).GridUid;
        var destGrid = Transform(destination).GridUid;
        var destName = Name(destination);

        // Шаттл может висеть в отдельной сетке — корабль определяем по маркеру.
        var onShipTwo = shuttleGrid != null && HasComp<SquadWarShipTwoComponent>(shuttleGrid.Value);
        var allowedLz = onShipTwo ? rule.SquadWarLZ1 : rule.SquadWarLZ2;

        if (destName.Contains(allowedLz, StringComparison.OrdinalIgnoreCase))
            return true;

        // Свой Алмаер (док) — всегда доступен, чтобы вернуться с планеты.
        var ownShipGrid = onShipTwo ? rule.ShipTwoGrid : rule.ShipOneGrid;
        if (destGrid == ownShipGrid)
            return true;

        var isEnemyShip = onShipTwo ? destGrid == rule.ShipOneGrid : destGrid == rule.ShipTwoGrid;
        var enemyUnlocked = onShipTwo ? rule.SquadWarShipTwoUnlocked : rule.SquadWarShipOneUnlocked;

        return isEnemyShip && enemyUnlocked;
    }

    public void SetSquadWarRechargeTime(EntityUid shuttle)
    {
        var comp = EnsureComp<DropshipComponent>(shuttle);
        comp.RechargeTime = TimeSpan.FromMinutes(5);
        Dirty(shuttle, comp);
    }
}
