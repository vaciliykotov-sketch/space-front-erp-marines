using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Mortar;

public sealed partial class MortarComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MinimumRange = 2;

    [DataField, AutoNetworkedField]
    public int MaximumRange = 1000;

    [DataField, AutoNetworkedField]
    public int[] FireRandomOffset = new[] { 0 };
}
