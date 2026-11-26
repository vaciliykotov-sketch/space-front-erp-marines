using Content.Server.Administration.Logs;
using Content.Server.Cargo.Components;
using Content.Server.Chat.Systems;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Chasm;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.UserInterface;
using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared._RMC14.Xenonids;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;
using System.Runtime.InteropServices;
using static Content.Shared._RMC14.Requisitions.Components.RequisitionsElevatorMode;

namespace Content.Server._RMC14.Requisitions;

public sealed partial class RequisitionsSystem : SharedRequisitionsSystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private bool IsVehicleOrder(EntProtoId crateId)
    {
        return _prototype.TryIndex(crateId, out var proto) &&
               proto.Components!.TryGetComponent("RMCSpawnVehicleOnOrder", out _);
    }
}
