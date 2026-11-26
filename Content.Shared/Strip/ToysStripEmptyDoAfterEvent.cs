using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Strip;

[Serializable, NetSerializable]
public sealed partial class ToysStripEmptyDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public string Slot = string.Empty;
}
