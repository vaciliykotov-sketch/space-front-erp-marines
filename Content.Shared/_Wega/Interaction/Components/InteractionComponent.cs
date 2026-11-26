using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Interaction;

[RegisterComponent]
public sealed partial class InteractionActionsComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<InteractionActionPrototype>, TimeSpan> Cooldowns = new();
}
