using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.NamedItems;
using Content.Shared._RMC14.Xenonids.Name;
using Content.Shared._SpaceFront.TTS;
using Robust.Shared.Collections;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Shared.Preferences;

    public sealed partial class HumanoidCharacterProfile : ICharacterProfile
{
        [DataField]
        public string Voice { get; set; } = SharedHumanoidAppearanceSystem.DefaultVoice;
        // Stories-TTS-End

        public HumanoidCharacterProfile WithVoice(string voice)
        {
            return new(this) { Voice = voice };
        }
        // Stories-TTS-End

        public static bool CanHaveVoice(TTSVoicePrototype voice, Sex sex)
        {
            return voice.RoundStart && sex == Sex.Unsexed || (voice.Sex == sex || voice.Sex == Sex.Unsexed);
        }
        // Stories-TTS-End
}
