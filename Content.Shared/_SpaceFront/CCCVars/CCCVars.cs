using Robust.Shared.Configuration;
using Content.Shared._SpaceFront.TTS;

namespace Content.Shared._SpaceFront.CCCVars;

// ReSharper disable once InconsistentNaming
[CVarDefs]
public sealed class CCCVars
{
    public static readonly CVarDef<string> TTSApiUrl =
        CVarDef.Create("tts.api_url", "http://127.0.0.1:5000", CVar.SERVERONLY);

    public static readonly CVarDef<string> TTSApiToken =
        CVarDef.Create("tts.api_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<int> TTSApiTimeout =
        CVarDef.Create("tts.api_timeout", 20, CVar.SERVERONLY);

    public static readonly CVarDef<int> TTSMaxCache =
        CVarDef.Create("tts.max_cache", 200, CVar.SERVERONLY);

    public static readonly CVarDef<bool> TTSEnabled =
        CVarDef.Create("tts.enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Volume of TTS radio messages.
    /// </summary>
    public static readonly CVarDef<float> TTSRadioVolume =
        CVarDef.Create("tts.radio_volume", 1.2f, CVar.CLIENTONLY);

    /// <summary>
    /// Default volume setting of TTS sound.
    /// </summary>
    public static readonly CVarDef<float> TTSVolume =
        CVarDef.Create("tts.volume", 1.2f, CVar.CLIENTONLY);

    /// <summary>
    /// TTS voice effect preset. 0 = None.
    /// </summary>
    public static readonly CVarDef<int> TTSVoiceEffect =
        CVarDef.Create("tts.voice_effect", 0, CVar.CLIENTONLY);

    public static readonly CVarDef<int> TTSRadioFilter =
        CVarDef.Create("tts.radio_filter", (int) RadioChannelFlag.All, CVar.CLIENTONLY);

    public static readonly CVarDef<float> TTSRateLimitPeriod =
        CVarDef.Create("tts.rate_limit_period", 10f, CVar.SERVERONLY);

    public static readonly CVarDef<int> TTSRateLimitCount =
        CVarDef.Create("tts.rate_limit_count", 5, CVar.SERVERONLY);
}
