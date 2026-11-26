using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Inventory.Events;

[Serializable, NetSerializable]
public sealed partial class ToysSlotDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public string Slot = string.Empty;
}
