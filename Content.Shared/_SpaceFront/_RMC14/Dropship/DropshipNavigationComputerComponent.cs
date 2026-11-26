using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Dropship;

public sealed partial class DropshipNavigationComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool RemoteControl = true; // Space Front: всегда включено
}
