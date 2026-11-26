using Content.Shared._SpaceFront.Respawn;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client._SpaceFront.Respawn;

/// <summary>
/// Space Front: показывает неотключаемое окно смерти призрака с таймером.
/// Данные приходят в обычном RespawnResetEvent (поле DeathWindowSeconds).
/// </summary>
public sealed class SquadWarDeathWindowSystem : EntitySystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private SquadWarDeathWindow? _window;

    // True, пока окно смерти должно быть видно (счётчик ещё идёт).
    private bool _windowActive;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RespawnResetEvent>(OnRespawnReset);
    }

    private void OnRespawnReset(RespawnResetEvent msg)
    {
        Log.Info($"SF deathwindow diag: event received, DeathWindowSeconds={msg.DeathWindowSeconds}, Time={msg.Time}");

        if (msg.DeathWindowSeconds < 0)
        {
            _windowActive = false;
            _window?.Dispose();
            _window = null;
            return;
        }

        if (msg.DeathWindowSeconds <= 0)
            return; // обычный таймер возрождения — окно не трогаем

        _windowActive = true;

        // Переход «тело → призрак» пересобирает UI и отсоединяет/уничтожает окно,
        // поэтому пересоздаём его, если оно больше не в дереве.
        if (_window == null || _window.Disposed || _window.Parent == null)
        {
            _window = new SquadWarDeathWindow();
            _ui.RootControl.AddChild(_window);
            _window.SetPositionLast(); // ПОВЕРХ экрана: SetPositionFirst прятал окно под затемнением смерти
            LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.Center);
        }

        _window.SecondsLeft = msg.DeathWindowSeconds;
        _window.UpdateTimer();
        LogWindowState("after-event");
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        // UI может пересобраться уже ПОСЛЕ события (переход «тело → призрак»)
        // и отсоединить окно — возвращаем его на место, пока счётчик активен.
        if (!_windowActive)
            return;

        if (_window != null && _timing.CurTime.TotalSeconds >= _nextDiag)
        {
            _nextDiag = _timing.CurTime.TotalSeconds + 5;
            LogWindowState("frame");
        }

        if (_window == null || _window.Disposed || _window.Parent == null)
        {
            _window = new SquadWarDeathWindow();
            _window.SecondsLeft = _windowActive ? _lastSeconds : 0;
            _ui.RootControl.AddChild(_window);
            _window.SetPositionLast();
            LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.Center);
            _window.UpdateTimer();
        }
        else if (_window.SecondsLeft > 0)
        {
            _lastSeconds = _window.SecondsLeft;
        }
    }

    private float _lastSeconds;

    private double _nextDiag;

    private void LogWindowState(string tag)
    {
        if (_window == null)
        {
            Log.Info($"SF deathwindow: {tag} window=null active={_windowActive}");
            return;
        }

        Log.Info($"SF deathwindow: {tag} inTree={_window.Parent != null} disposed={_window.Disposed} " +
                 $"visible={_window.Visible} size={_window.Size.X}x{_window.Size.Y} pos={_window.Position.X},{_window.Position.Y} " +
                 $"rootChildren={_ui.RootControl.ChildCount}");
    }
}
