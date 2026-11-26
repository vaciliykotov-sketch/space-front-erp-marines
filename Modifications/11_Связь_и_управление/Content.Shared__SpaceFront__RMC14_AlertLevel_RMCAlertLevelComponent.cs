using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.AlertLevel;

public sealed partial class RMCAlertLevelComponent : Component
{
    public Dictionary<EntityUid, RMCAlertLevels> MapLevels = new();
}
