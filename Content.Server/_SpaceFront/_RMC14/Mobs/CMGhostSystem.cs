using Content.Server.Ghost;
using Content.Server._SpaceFront.Respawn;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Content.Shared.Overlays;
using Content.Shared._RMC14.Admin;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Mobs;
using Content.Shared._RMC14.PropCalling;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server._RMC14.Mobs;

    public sealed partial class CMGhostSystem : SharedCMGhostSystem
{
        [Dependency] private readonly RespawnSystem _respawnSystem = default!;

        [Dependency] private readonly IPlayerManager _player = default!;
}
