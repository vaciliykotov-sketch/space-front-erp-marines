using Robust.Shared.GameStates;
using Content.Shared._SpaceFront.Rules;

namespace Content.Shared._SpaceFront.Rules;

/// <summary>
/// SquadWar: знак команды игрока — «АБ» (Альфа/Браво) или «ДЧ» (Чарли/Дельта).
/// Показывается в осмотре и используется для определения победителя.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SquadWarTeamComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Team = "АБ";
}
