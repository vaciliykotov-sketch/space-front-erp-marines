using Content.Client.UserInterface.Controls;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines.ControlComputer;
using Content.Shared._RMC14.Overwatch;
using Content.Shared._RMC14.TacticalMap;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Marines.Announce;

[UsedImplicitly]
// Stories-start: класс сделан partial для выноса в _SpaceFront
public sealed partial class MarineCommunicationsComputerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
// Stories-end: класс сделан partial для выноса в _SpaceFront
{
    [ViewVariables]
    private MarineCommunicationsComputerWindow? _window;

    private MarineAnnounceSystem? _marineAnnounce;

    private bool _confirmingEvacuation;

    // Stories-start: изменённый член (наша версия, см. PATCHES.md)
    protected override void Open()
    {
        base.Open();
        if (_window != null)
            return;

        _window = this.CreateWindow<MarineCommunicationsComputerWindow>();

        if (EntMan.TryGetComponent(Owner, out MarineCommunicationsComputerComponent? communications) &&
            communications.CanGiveMedals)
        {
            _window.MedalButton.OnPressed += _ => SendPredictedMessage(new MarineControlComputerOpenMedalsPanelMsg());
            _window.MedalButton.Visible = true;
        }
        else
        {
            _window.MedalButton.Visible = false;
        }

        if (EntMan.HasComponent<TacticalMapComputerComponent>(Owner))
            _window.TacticalMapButton.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsOpenMapMsg());
        else
            _window.TacticalMapButton.Visible = false;

        if (EntMan.TryGetComponent(Owner, out MarineCommunicationsComputerComponent? communicationsEvac) &&
            communicationsEvac.CanInitiateEvac)
        {
            _window.EvacuationButton.OnPressed += _ =>
            {
                if (_confirmingEvacuation)
                {
                    SendPredictedMessage(new MarineControlComputerToggleEvacuationMsg());
                    _confirmingEvacuation = false;
                }
                else
                {
                    _confirmingEvacuation = true;
                }

                OnStateUpdate();
            };
            _window.EvacuationButton.Visible = true;
        }
        else
        {
            _window.EvacuationButton.Visible = false;
        }

        if (EntMan.HasComponent<OverwatchConsoleComponent>(Owner))
            _window.OverwatchButton.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsOverwatchMsg());
        else
            _window.OverwatchButton.Visible = false;

        _window.Text.OnTextChanged += args => OnTextChanged((int) Rope.CalcTotalLength(args.TextRope));

        _window.Send.OnPressed += _ => SendPredictedMessage(new MarineCommunicationsComputerMsg( Rope.Collapse(_window.Text.TextRope)));
        OnStateUpdate();
        OnTextChanged(0);
    }
    // Stories-end

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        OnStateUpdate();
    }

    // Stories-start: изменённый член (наша версия, см. PATCHES.md)
    public void OnStateUpdate()
    {
        if (_window == null)
            return;

        if (State is MarineCommunicationsComputerBuiState s)
        {
            _window.PlanetName.Text = s.Planet;
            _window.OperationName.Text = s.Operation;
        }

        if (EntMan.TryGetComponent(Owner, out MarineControlComputerComponent? evaccomputer))
        {
            // TODO RMC14 estimated time until escape pod launch
            if (_confirmingEvacuation)
                _window.EvacuationButton.Text = "Подтвердить?";
            else
                _window.EvacuationButton.Text = evaccomputer.Evacuating ? "Отменить эвакуацию" : "Начать эвакуацию";

            _window.EvacuationButton.Disabled = !evaccomputer.CanEvacuate;
        }
    }
    // Stories-end

    private void OnTextChanged(int textLength)
    {
        if (_window == null)
            return;

        _marineAnnounce ??= EntMan.System<MarineAnnounceSystem>();
        _window.CharacterCount.Text = $"{textLength} / {_marineAnnounce.CharacterLimit}";
        _window.Send.Disabled = textLength == 0;
    }
}
