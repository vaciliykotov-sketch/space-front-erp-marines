using Content.Shared.Audio;
using Content.Shared.CCVar;
using Content.Shared.Coordinates;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Doors;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Content.Shared.Maps;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Prying.Components;
using Content.Shared.UserInterface;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.HyperSleep;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Xenonids.Announce;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Content.Shared._RMC14.Evacuation;

public abstract partial class SharedEvacuationSystem : EntitySystem
{
    public void ForceEvacuationCrashState(EntityUid map)
    {
        var progress = EnsureComp<EvacuationProgressComponent>(map);
        progress.DropShipCrashed = true;
        Dirty(map, progress);

        var doors = EntityQueryEnumerator<EvacuationDoorComponent, DoorComponent>();
        while (doors.MoveNext(out var uid, out var door, out var doorComp))
        {
            door.Locked = false;
            Dirty(uid, door);
        }

        if (IsEvacuationEnabled())
            OpenEvacuationDoors();
    }
}
