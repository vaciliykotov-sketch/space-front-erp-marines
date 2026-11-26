using Robust.Shared.GameStates;
using Content.Shared._SpaceFront.Rules;

namespace Content.Shared._SpaceFront.Rules;

/// <summary>
/// Маркер: сущность принадлежит второму Алмаеру (команда Альфа/Браво).
/// Используется для разделения тактической карты и других механик по командам.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SquadWarShipTwoComponent : Component
{
}
