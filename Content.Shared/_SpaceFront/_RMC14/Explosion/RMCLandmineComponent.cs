using Content.Shared.Tools;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Explosion;

public sealed partial class RMCLandmineComponent : Component
{
    [DataField]
    public string? Team;
}
