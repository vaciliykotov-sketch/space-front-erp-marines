using Content.Shared._SpaceFront.TTS;
using Content.Shared.Implants;
using Content.Shared.Inventory;
using Content.Server.VoiceMask;
using Content.Shared.VoiceMask;

namespace Content.Server.VoiceMask;

public partial class VoiceMaskSystem
{
    private void OnChangeVoice(Entity<VoiceMaskComponent> entity, ref VoiceMaskChangeVoiceMessage msg)
    {
        if (msg.Voice is { } id && !_proto.HasIndex<TTSVoicePrototype>(id))
            return;

        entity.Comp.VoiceId = msg.Voice;
        UpdateUI(entity);
    }

    private void OnSpeakerVoiceTransform(Entity<VoiceMaskComponent> entity, ref InventoryRelayedEvent<TransformSpeakerVoiceEvent> args)
    {
        args.Args.VoiceId = entity.Comp.VoiceId ?? string.Empty;
    }

    private void OnSpeakerVoiceTransformImplant(Entity<VoiceMaskComponent> entity, ref ImplantRelayEvent<TransformSpeakerVoiceEvent> args)
    {
        args.Event.VoiceId = entity.Comp.VoiceId ?? string.Empty;
    }
}
