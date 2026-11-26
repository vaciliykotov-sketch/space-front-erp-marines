using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared._SpaceFront.Vehicle;

namespace Content.Shared._SpaceFront.Vehicle;

/// <summary>
/// Ящик заказа техники: при доставке на грузовом подъёмнике
/// спавнит машину по центру платформы и кладёт модули в ящик.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCSpawnVehicleOnOrderComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Vehicle;

    [DataField]
    public List<EntProtoId> Hardpoints = new();
}
