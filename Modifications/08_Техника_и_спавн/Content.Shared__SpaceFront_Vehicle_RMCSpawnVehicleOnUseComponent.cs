using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared._SpaceFront.Vehicle;

namespace Content.Shared._SpaceFront.Vehicle;

/// <summary>
/// При использовании спавнит технику рядом с пользователем.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCSpawnVehicleOnUseComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Vehicle;
}
