using Robust.Shared.Prototypes;

namespace Content.Shared.Radio;

public sealed partial class RadioChannelPrototype : IPrototype
{
    [DataField]
    public bool ShowInTTSOptions { get; private set; } = true;

    [DataField]
    public string? TtsCategory { get; private set; }
}
