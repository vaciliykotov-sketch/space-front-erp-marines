using Content.Shared.CCVar;
using Content.Shared.Decals;
using Content.Shared.Examine;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared._RMC14.Humanoid;
using Content.Shared._SpaceFront.TTS;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects.Components.Localization;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using Robust.Shared;
using System.IO;
using System.Linq;
using System.Numerics;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.Humanoid;

public abstract partial class SharedHumanoidAppearanceSystem : EntitySystem
{
    public const string DefaultVoice = "father_grigori";

    public static readonly Dictionary<Sex, string> DefaultSexVoice = new()
    {
        { Sex.Male, "father_grigori" },
        { Sex.Female, "alyx" },
        { Sex.Unsexed, "adventure_core" },
    };
    // Stories-TTS-End

    public void SetTTSVoice(EntityUid uid, string voiceId, HumanoidAppearanceComponent humanoid)
    {
        var comp = EnsureComp<TTSComponent>(uid);

        humanoid.Voice = voiceId;
        if (string.IsNullOrEmpty(voiceId))
            return; // Space Front: не затирать голос, заданный в прототипе

        comp.VoicePrototypeId = voiceId;
    }
    // Stories-TTS-End
}
