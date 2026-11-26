using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Whitelist;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Station;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Content.Shared._RMC14.Humanoid;

public sealed partial class RMCHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    private void OnHumanoidMapInit(EntityUid uid, HumanoidAppearanceComponent comp, MapInitEvent args)
    {
        Log.Info($"SF diag: mapinit — sex={comp.Sex}, inv={HasComp<InventoryComponent>(uid)}");
        if (comp.Sex == Sex.Female && TryComp<InventoryComponent>(uid, out var inv))
            _inventory.EnsureToys2Slot((uid, inv));
    }

    private void OnPlayerAttached(EntityUid uid, HumanoidAppearanceComponent comp, PlayerAttachedEvent args)
    {
        Log.Info($"SF diag: attached — sex={comp.Sex}, inv={HasComp<InventoryComponent>(uid)}");
        if (comp.Sex == Sex.Female && TryComp<InventoryComponent>(uid, out var inv))
            _inventory.EnsureToys2Slot((uid, inv));
    }
}
