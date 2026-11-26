using Content.Shared.Inventory.Events;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.Inventory;

public partial class InventorySystem : EntitySystem
{
    public void EnsureToys2Slot(Entity<InventoryComponent> ent)
    {
        if (ent.Comp.Slots.Any(s => s.Name == "toys2"))
            return;

        var slotDef = _prototypeManager.Index<InventoryTemplatePrototype>("human_female").Slots.FirstOrDefault(s => s.Name == "toys2");
        if (slotDef == null)
            return;

        var slots = ent.Comp.Slots.ToList();
        slots.Add(slotDef);
        ent.Comp.Slots = slots.ToArray();

        var containers = ent.Comp.Containers.ToList();
        containers.Add(_containerSystem.EnsureContainer<ContainerSlot>(ent.Owner, "toys2"));
        ent.Comp.Containers = containers.ToArray();

        ent.Comp.TemplateId = "human_female";

        var ev = new InventoryTemplateUpdated();
        RaiseLocalEvent(ent, ref ev);
        Dirty(ent);

        Log.Info($"SF diag: toys2 slot added — uid={ent.Owner}, slots={ent.Comp.Slots.Length}");
    }

    public void OverrideTemplateIdBeforeInit(Entity<InventoryComponent> ent, ProtoId<InventoryTemplatePrototype> newTemplate)
    {
        ent.Comp.TemplateId = newTemplate;
    }
}
