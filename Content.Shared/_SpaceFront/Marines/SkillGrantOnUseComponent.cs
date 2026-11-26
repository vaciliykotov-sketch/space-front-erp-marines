using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared._SpaceFront.Marines;

namespace Content.Shared._SpaceFront.Marines;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SkillsSystem))]
public sealed partial class SkillGrantOnUseComponent : Component
{
    [DataField(required: true)]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> Skills = new();
}
