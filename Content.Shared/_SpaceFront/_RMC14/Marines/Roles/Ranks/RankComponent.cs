using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Roles.Ranks;

public sealed partial class RankComponent : Component
{
    [DataField, AutoNetworkedField]
    public int OverallPlaytimeHours;
}
