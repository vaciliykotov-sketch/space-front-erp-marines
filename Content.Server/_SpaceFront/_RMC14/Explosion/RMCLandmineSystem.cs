using Content.Server.Explosion.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Systems;
using Content.Shared.StepTrigger.Systems;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids.Projectile;
using Robust.Shared.Physics.Events;

namespace Content.Server._RMC14.Explosion;

public sealed partial class RMCLandmineSystem : SharedRMCLandmineSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
}
