using Content.Shared.DisplacementMap;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._SpaceFront.TTS;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidAppearanceComponent : Component, IRMCHumanoidAppearance
{
    [DataField("voice")]
    public ProtoId<TTSVoicePrototype> Voice { get; set; } = SharedHumanoidAppearanceSystem.DefaultVoice;
    // Stories-TTS-End
}
