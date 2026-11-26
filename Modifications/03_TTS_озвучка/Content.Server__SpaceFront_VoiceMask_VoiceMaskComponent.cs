using Content.Shared.Speech;
using Robust.Shared.Prototypes;

namespace Content.Server.VoiceMask;

public sealed partial class VoiceMaskComponent : Component
{
    [DataField]
    public string? VoiceId;
}
