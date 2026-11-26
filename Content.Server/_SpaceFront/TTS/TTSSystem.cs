using System.Linq;
using System.Threading.Tasks;
using Content.Server.Communications;
using Content.Server.Power.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Popups;
using Content.Shared._SpaceFront.CCCVars;
using Content.Shared._SpaceFront.TTS;
using Content.Shared.GameTicking;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Station.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Speech.Muting;
using Content.Shared.Ghost;
using Content.Shared.Speech;

namespace Content.Server._SpaceFront.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private INetConfigurationManager _netCfg = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private TTSManager _ttsManager = default!;
    [Dependency] private StationSystem _stationSystem = default!;
    [Dependency] private IRobustRandom _rng = default!;

    private ISawmill _sawmill = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    // Space Front: кулдаун озвучки (наша разработка) — «Подожди!» пока речь играет.
    private readonly Dictionary<EntityUid, TimeSpan> _ttsSpeakCooldown = new();

    private readonly HashSet<string> _sampleText = new()
    {
        // Space Front: фраза для превью голоса.
        "Так звучит мой голос.",
    };

    private static readonly ProtoId<TTSVoicePrototype> AnnouncementSpeaker = "Glados";
    private const int MaxMessageChars = 100 * 2; // same as SingleBubbleCharLimit * 2
    private const float AnnouncementDelay = 2.25f;
    private const float TTSRange = 10.5f;
    private bool _isEnabled;

    public override void Initialize()
    {
        _cfg.OnValueChanged(CCCVars.TTSEnabled, v => _isEnabled = v, true);

        _sawmill = Logger.GetSawmill("tts");
        _ttsManager.Initialize();

        RegisterRateLimits();

        SubscribeLocalEvent<SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<RequestPreviewTTSEvent>(OnRequestPreviewTTS);
        SubscribeLocalEvent<CommunicationConsoleAnnouncementEvent>(OnConsoleAnnouncement);
        SubscribeLocalEvent<EntitySpokeEvent>(OnEntitySpoke);
    }

    // Space Front: не даём говорить, пока предыдущая озвучка ещё играет.
    private void OnSpeakAttempt(SpeakAttemptEvent ev)
    {
        if (!_ttsSpeakCooldown.TryGetValue(ev.Uid, out var until) || _timing.CurTime >= until)
            return;

        ev.Cancel();
        _popup.PopupEntity(Loc.GetString("tts-speak-cooldown"), ev.Uid, ev.Uid);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
        => _ttsManager.ResetCache();

    private async void OnRequestPreviewTTS(RequestPreviewTTSEvent ev, EntitySessionEventArgs args)
    {
        if (!_isEnabled || !_proto.TryIndex<TTSVoicePrototype>(ev.VoiceId, out var protoVoice))
            return;

        if (HandleRateLimit(args.SenderSession) != RateLimitStatus.Allowed)
            return;

        var previewText = _rng.Pick(_sampleText);
        var soundData = await GenerateTTS(previewText, protoVoice.Speaker);
        if (soundData is null)
            return;

        RaiseNetworkEvent(
            new PlayTTSEvent(soundData),
            Filter.SinglePlayer(args.SenderSession),
            recordReplay: false);
    }

    private void OnConsoleAnnouncement(ref CommunicationConsoleAnnouncementEvent ev)
    {
        if (!_isEnabled || string.IsNullOrEmpty(ev.Text))
            return;

        var station = _stationSystem.GetOwningStation(ev.Sender);
        if (station == null)
            return;

        TTSVoicePrototype? voicePrototype = null;
        if (TryComp<TTSComponent>(ev.Sender, out var ttsComp) && !HasComp<MutedComponent>(ev.Sender))
        {
            if (!string.IsNullOrEmpty(ttsComp.VoicePrototypeId))
            {
                _proto.TryIndex(ttsComp.VoicePrototypeId, out voicePrototype);
            }
        }

        if (voicePrototype == null)
        {
            if (!_proto.TryIndex(AnnouncementSpeaker, out voicePrototype))
                return;
        }

        HandleConsoleAnnouncement(ev.Text, voicePrototype.Speaker, ev.Component.Sound, station.Value);
    }

    private async void HandleConsoleAnnouncement(
        string text,
        string speaker,
        SoundSpecifier sound,
        EntityUid station)
    {
        var textSanitized = Sanitize(text);
        if (string.IsNullOrEmpty(textSanitized))
            return;

        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        var ssmlTraits = SoundTraits.RateFast | SoundTraits.PitchMedium;
        var textSsml = ToSsmlText(textSanitized, ssmlTraits);

        var soundData = await _ttsManager.ConvertTextToSpeech(speaker, textSsml);
        if (soundData is null)
            return;

        var timeDelay = (float)_audio.GetAudioLength(_audio.ResolveSound(sound)).TotalSeconds + AnnouncementDelay;

        Timer.Spawn(TimeSpan.FromSeconds(timeDelay),
            () =>
        {
            var filter = GetStationFilter(station);
            if (filter == null)
                return;

            RaiseNetworkEvent(new PlayTTSEvent(soundData),
                filter,
                recordReplay: false);
        });
    }

    private Filter? GetStationFilter(EntityUid? stationUid)
    {
        return stationUid is { } uid ? _stationSystem.GetInOwningStation(uid) : null;
    }

    private void OnEntitySpoke(EntitySpokeEvent args)
    {
        var uid = args.Source;
        if (!TryComp<TTSComponent>(uid, out var ttsComp))
            return;

        _sawmill.Info($"SF diag: TTS spoke uid={uid} voice={ttsComp.VoicePrototypeId} msg={args.Message.Substring(0, Math.Min(30, args.Message.Length))}");

        var voiceId = ttsComp.VoicePrototypeId;
        if (!_isEnabled || string.IsNullOrEmpty(voiceId))
            return;

        if (args.Message.Length > MaxMessageChars)
            return;

        var voiceEv = new TransformSpeakerVoiceEvent(uid, voiceId);
        RaiseLocalEvent(uid, voiceEv);
        voiceId = voiceEv.VoiceId;

        if (!_proto.TryIndex(voiceId, out var protoVoice))
            return;

        // Space Front: оценка длительности будущей озвучки.
        if (args.Message.Length > 0)
        {
            var estimate = TimeSpan.FromSeconds(Math.Max(1.2, args.Message.Length / 12.0));
            _ttsSpeakCooldown[uid] = _timing.CurTime + estimate;
        }

        if (args.ObfuscatedMessage != null)
        {
            HandleWhisper(uid, args.Message, protoVoice.Speaker, args.Channel);
            return;
        }

        HandleSay(uid, args.Message, protoVoice.Speaker, args.Channel);
    }

    private async void HandleSay(EntityUid uid, string message, string speaker, RadioChannelPrototype? channel)
    {
        var soundData = await GenerateTTS(message, speaker);

        if (soundData is null)
        {
            _sawmill.Info($"SF diag: TTS synth FAILED for uid={uid}");
            return;
        }

        _sawmill.Info($"SF diag: TTS synth ok uid={uid} bytes={soundData.Length}");

        // Space Front: точный кулдаун по реальной длительности озвучки.
        var exactDuration = GetOggDuration(soundData);
        if (exactDuration > TimeSpan.Zero)
            _ttsSpeakCooldown[uid] = _timing.CurTime + exactDuration + TimeSpan.FromMilliseconds(300);

        // Should be here because EntitySpokeEvent may be called on the entities in the PVS range, but not always in range where player receives chat messages
        var filter = GetReceiversFilter(uid, TTSRange);
        if (filter.Recipients.Any())
        {
            RaiseNetworkEvent(new PlayTTSEvent(soundData, GetNetEntity(uid)), filter, recordReplay: false);
        }

    }

    private async void HandleWhisper(
        EntityUid uid,
        string message,
        string speaker,
        RadioChannelPrototype? channel)
    {
        var fullSoundData = await GenerateTTS(message, speaker, true);
        if (fullSoundData is null)
            return;

        var fullTtsEvent = new PlayTTSEvent(fullSoundData, GetNetEntity(uid), true);

        // TODO: Check obstacles
        var filter = GetReceiversFilter(uid, 4f);
        if (filter.Recipients.Any())
        {
            RaiseNetworkEvent(fullTtsEvent, filter, recordReplay: false);
        }

    }

    /// <inheritdoc cref="TelecomServerComponent"/>
    private bool HasActiveServer(MapId mapId, string channelId)
    {
        var servers = EntityQuery<TelecomServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        foreach (var (_, keys, power, transform) in servers)
        {
            if (transform.MapID == mapId && power.Powered
                && keys.Channels.Contains(channelId))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Maps channel prototype ID to RadioChannelFlag.
    /// </summary>
    private static RadioChannelFlag GetChannelFlag(string channelId)
    {
        return channelId switch
        {
            "Common" => RadioChannelFlag.Common,
            "Command" => RadioChannelFlag.Command,
            "Engineering" => RadioChannelFlag.Engineering,
            "Medical" => RadioChannelFlag.Medical,
            "Science" => RadioChannelFlag.Science,
            "Security" => RadioChannelFlag.Security,
            "Service" => RadioChannelFlag.Service,
            "Supply" => RadioChannelFlag.Supply,
            "Legal" => RadioChannelFlag.Legal,
            "Syndicate" => RadioChannelFlag.Syndicate,
            "Binary" => RadioChannelFlag.Binary,
            "Handheld" => RadioChannelFlag.Handheld,
            "Freelance" => RadioChannelFlag.Freelance,
            "CentCom" => RadioChannelFlag.CentCom,
            "Xenoborg" => RadioChannelFlag.Xenoborg,
            "Mothership" => RadioChannelFlag.Mothership,
            _ => RadioChannelFlag.None,
        };
    }

    // TODO: Check obstacles
    private Filter GetReceiversFilter(EntityUid sourceUid, float range)
    {
        var pvs = Filter.Pvs(sourceUid);
        var filter = Filter.Empty();

        foreach (var player in pvs.Recipients)
        {
            if (player.AttachedEntity is not { Valid: true } playerEntity)
                continue;

            var transformEntity = Transform(playerEntity);

            if (transformEntity.MapID != Transform(sourceUid).MapID)
                continue;

            if (!Transform(sourceUid).Coordinates.TryDistance(EntityManager, transformEntity.Coordinates, out var distance)
                || distance >= range)
                continue;

            filter.AddPlayer(player);
        }

        return filter;
    }

    // ReSharper disable once InconsistentNaming
    private async Task<byte[]?> GenerateTTS(string text, string speaker, bool isWhisper = false)
    {
        var textSanitized = Sanitize(text);

        if (string.IsNullOrEmpty(textSanitized))
            return null;

        if (char.IsLetter(textSanitized[^1]))
            textSanitized += ".";

        SoundTraits ssmlTraits;
        if (isWhisper)
        {
            ssmlTraits = SoundTraits.RateSlow | SoundTraits.PitchVerylow | SoundTraits.VolumeXSoft;
        }
        else
        {
            ssmlTraits = SoundTraits.RateFast | SoundTraits.PitchMedium;
        }

        // Space Front: NTTS принимает простой текст (без SSML-разметки).
        return await _ttsManager.ConvertTextToSpeech(speaker, textSanitized);
    }

    /// <summary>
    /// Длительность OGG-аудио из байтов (наша разработка — для точного кулдауна).
    /// </summary>
    private static TimeSpan GetOggDuration(byte[] data)
    {
        try
        {
            const int opusRate = 48000;
            long granule = -1;

            for (var pos = 0; pos + 27 < data.Length;)
            {
                if (data[pos] != (byte) 'O' || data[pos + 1] != (byte) 'g' ||
                    data[pos + 2] != (byte) 'g' || data[pos + 3] != (byte) 'S')
                {
                    break;
                }

                var headerType = data[pos + 5];
                var pageGranule = BitConverter.ToInt64(data, pos + 6);
                if (pageGranule > 0)
                    granule = pageGranule;

                if ((headerType & 0x04) != 0)
                    break;

                var segments = data[pos + 26];
                var bodySize = 0;
                for (var s = 0; s < segments; s++)
                    bodySize += data[pos + 27 + s];

                pos += 27 + segments + bodySize;
            }

            if (granule > 0)
                return TimeSpan.FromSeconds(granule / (double) opusRate);
        }
        catch
        {
        }

        return TimeSpan.Zero;
    }
}

