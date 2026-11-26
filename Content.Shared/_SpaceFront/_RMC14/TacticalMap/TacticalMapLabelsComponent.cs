using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared._RMC14.TacticalMap;

public sealed partial class TacticalMapLabelsComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<Vector2i, string> MarineLabelsShipTwo = new();
}
