using Content.Client.Gameplay;
using Content.Client.Ghost;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.UserInterface.Systems.Ghost.Widgets;
using Content.Client._SpaceFront.Respawn;
using Content.Shared.Ghost;
using Robust.Client.Console;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface;

namespace Content.Client.UserInterface.Systems.Ghost;

public sealed partial class GhostUIController : UIController, IOnSystemChanged<GhostSystem>, IOnSystemChanged<RespawnSystem>
{
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;

    [UISystemDependency] private readonly RespawnSystem? _respawn = default;

    public void OnSystemLoaded(RespawnSystem system)
    {
        system.RespawnReseted += OnRespawnReseted;
    }

    public void OnSystemUnloaded(RespawnSystem system)
    {
        system.RespawnReseted -= OnRespawnReseted;
    }

    private void OnRespawnReseted()
    {
        Gui?.UpdateRespawn(_respawn?.RespawnResetTime);
    }

    private void GhostRespawnPressed()
    {
        _consoleHost.ExecuteCommand("ghostrespawn");
    }
}
