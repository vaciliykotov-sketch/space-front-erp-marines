using Content.Client._SpaceFront.TTS;
using Content.Shared._SpaceFront.CCCVars;
using Robust.Client.UserInterface;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private TTSTab? _ttsTab;

    private void RefreshVoiceTab()
    {
        if (!_cfgManager.GetCVar(CCCVars.TTSEnabled) || _ttsTab != null)
            return;

        _ttsTab = new TTSTab();
        TabContainer.AddChild(_ttsTab);
        TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-voice-tab"));

        _ttsTab.OnVoiceSelected += voiceId =>
        {
            SetVoice(voiceId);
            _ttsTab.SetSelectedVoice(voiceId);
        };

        _ttsTab.OnPreviewRequested += voiceId =>
        {
            _entManager.System<TTSSystem>().RequestPreviewTTS(voiceId);
        };
    }

    private void UpdateTTSVoicesControls()
    {
        if (Profile is null || _ttsTab is null)
            return;

        _ttsTab.UpdateControls(Profile, Profile.Voice);
        _ttsTab.SetSelectedVoice(Profile.Voice);
    }

    private void SetVoice(string newVoice)
    {
        Profile = Profile?.WithVoice(newVoice);
        IsDirty = true;
    }
}
