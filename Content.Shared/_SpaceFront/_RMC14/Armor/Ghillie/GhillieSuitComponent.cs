using Content.Shared.Inventory;
using Content.Shared.Whitelist;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor.Ghillie;

public sealed partial class GhillieSuitComponent : Component
{
    [DataField]
    public SlotFlags Slot = SlotFlags.OUTERCLOTHING;
}
