using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Shared._Wega.ToysLeash;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToysColorComponent : Component
{
    [DataField, AutoNetworkedField]
    public Color Color = Color.White;
}
