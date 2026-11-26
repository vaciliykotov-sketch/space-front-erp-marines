using Content.Shared.Administration.Logs;
using Content.Shared.Administration.Managers;
using Content.Shared.CombatMode;
using Content.Shared.Cuffs.Components;
using Content.Shared.Cuffs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Inventory;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Strip.Components;
using Content.Shared.Verbs;
using Content.Shared._RMC14.Clothing;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._Wega.ToysLeash;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Strip;

public abstract partial class SharedStrippableSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    [Dependency] private readonly PullingSystem _pulling = default!;

    [Dependency] private readonly MetaDataSystem _metadata = default!;

    private void OnToysStripEmptyDoAfter(ToysStripEmptyDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || _net.IsClient)
            return;

        args.Handled = true;

        var target = args.Args.Target;
        if (target is not { Valid: true })
            return;

        _inventorySystem.ShowToysSlotEffects(target.Value, args.Args.User);
    }
}
