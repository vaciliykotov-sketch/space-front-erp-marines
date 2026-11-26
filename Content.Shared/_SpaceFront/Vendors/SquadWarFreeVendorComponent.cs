using Robust.Shared.GameStates;
using Content.Shared._SpaceFront.Vendors;

namespace Content.Shared._SpaceFront.Vendors;

/// <summary>
/// Владелец может пользоваться любыми автоматизированными вендорами/шкафами КолМорТех
/// независимо от роли, ранга и доступа (режим SquadWar).
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SquadWarFreeVendorComponent : Component
{
}
