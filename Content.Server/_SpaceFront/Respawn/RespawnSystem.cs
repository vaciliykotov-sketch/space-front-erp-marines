using System.Runtime.InteropServices;
using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared._SpaceFront.Respawn;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Content.Shared.CCVar;
using Content.Server.Administration.Managers;

namespace Content.Server._SpaceFront.Respawn;

/// <summary>
/// Возрождение призрака в лобби (на базе механики Corvax/Frontier, адаптировано для Space Front).
/// После смерти игрок может вернуться в лобби только через RespawnTime секунд.
/// Повторно зайти за персонажа с тем же именем в этом раунде нельзя.
/// </summary>
public sealed class RespawnSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IChatManager _chat = default!;

    private sealed class RespawnData
    {
        public TimeSpan RespawnTime; // Время, после которого можно возродиться.
    }

    [ViewVariables]
    private Dictionary<NetUserId, RespawnData> _respawnInfo = new();

    // Space Front: пользователи, которых выкинем в лобби после окна смерти.
    private Dictionary<NetUserId, TimeSpan> _pendingDeathLobby = new();

    // Имена персонажей, использованные игроком в текущем раунде (запрет повтора).
    private Dictionary<NetUserId, HashSet<string>> _usedNames = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);
        SubscribeLocalEvent<ActorComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeNetworkEvent<SquadWarLeaveNowEvent>(OnLeaveNow);

        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    private void OnLeaveNow(SquadWarLeaveNowEvent ev, EntitySessionEventArgs args)
    {
        var userId = args.SenderSession.UserId;
        if (!_pendingDeathLobby.Remove(userId))
            return;

        if (args.SenderSession.Status != Robust.Shared.Enums.SessionStatus.Disconnected)
        {
            RaiseNetworkEvent(new RespawnResetEvent(null) { DeathWindowSeconds = -1 }, args.SenderSession);
            EntitySystem.Get<GameTicker>().Respawn(args.SenderSession);
        }
    }

    private void OnPlayerAttached(Entity<ActorComponent> ent, ref PlayerAttachedEvent args)
    {
        var userId = ent.Comp.PlayerSession.UserId;
        Log.Info($"respawn diag: OnPlayerAttached for {userId}, isGhost={HasComp<GhostComponent>(ent)}");

        if (TryComp<GhostComponent>(ent, out var ghost) && ghost.CanGhostInteract)
        {
            // Админ-призрак (/aghost) — окно не показываем, не выкидываем.
            return;
        }

        if (HasComp<GhostComponent>(ent))
        {
            // Игрок стал призраком — окно смерти и отсчёт 30 секунд.
            _pendingDeathLobby[userId] = _timing.CurTime + TimeSpan.FromSeconds(30);
            RaiseNetworkEvent(new RespawnResetEvent(null) { DeathWindowSeconds = 30 }, ent.Comp.PlayerSession);
        }
        else if (_pendingDeathLobby.Remove(userId))
        {
            // Вернулся в тело — окно закрываем, выкидывание отменяем.
            RaiseNetworkEvent(new RespawnResetEvent(null) { DeathWindowSeconds = -1 }, ent.Comp.PlayerSession);
        }
    }

    // При входе в игру отправляем актуальный таймер (если смерть уже была).
    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == Robust.Shared.Enums.SessionStatus.InGame &&
            _respawnInfo.TryGetValue(args.Session.UserId, out var data))
        {
            RaiseNetworkEvent(new RespawnResetEvent(data.RespawnTime), args.Session);
        }
    }

    /// <summary>
    /// Вызывается при смерти игрока (из CMGhostSystem). Ставит таймер до возрождения.
    /// </summary>
    public void OnPlayerDied(NetUserId userId)
    {
        var respawnData = GetRespawnData(userId);
        respawnData.RespawnTime = _timing.CurTime + TimeSpan.FromSeconds(_cfg.GetCVar(CCVars.RespawnTime));

        if (_player.TryGetSessionById(userId, out var sessionNow))
        {
            RaiseNetworkEvent(new RespawnResetEvent(respawnData.RespawnTime) { DeathWindowSeconds = 30 }, sessionNow);
            _pendingDeathLobby[userId] = _timing.CurTime + TimeSpan.FromSeconds(30);
            Log.Info($"respawn diag: timer set for {userId} until {respawnData.RespawnTime}, sent to session");

        }
        else
        {
            Log.Info($"respawn diag: timer set for {userId} but NO session to send");
        }
    }

    // После спавна: запоминаем имя персонажа; повторный вход с тем же именем — обратно в лобби.
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        var userId = ev.Player.UserId;
        var name = ev.Profile.Name;

        if (!_usedNames.TryGetValue(userId, out var names))
        {
            names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _usedNames[userId] = names;
        }

        if (!names.Add(name))
        {
            // Имя уже использовано в этом раунде — возвращаем в лобби.
            _chat.DispatchServerMessage(ev.Player, Loc.GetString("rmc-respawn-name-blocked", ("name", name)));
            EntitySystem.Get<GameTicker>().Respawn(ev.Player);
            return;
        }

        _chat.DispatchServerMessage(ev.Player, Loc.GetString("rmc-respawn-used-names", ("name", name), ("count", names.Count)));
    }

    public void RespawnUsedNamesClear(NetUserId user)
    {
        _usedNames.Remove(user);
    }

    /// <summary>
    /// True, если игрок уже использовал персонажа с таким именем в этом раунде.
    /// </summary>
    public bool IsNameBlocked(NetUserId userId, string name)
    {
        return _usedNames.TryGetValue(userId, out var names) && names.Contains(name);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        List<NetUserId>? toLobby = null;
        foreach (var (userId, until) in _pendingDeathLobby)
        {
            if (now < until)
                continue;

            toLobby ??= new List<NetUserId>();
            toLobby.Add(userId);
        }

        if (toLobby == null)
            return;

        foreach (var userId in toLobby)
        {
            _pendingDeathLobby.Remove(userId);
            if (_player.TryGetSessionById(userId, out var session) &&
                session.Status != Robust.Shared.Enums.SessionStatus.Disconnected)
            {
                RaiseNetworkEvent(new RespawnResetEvent(null) { DeathWindowSeconds = -1 }, session);
                EntitySystem.Get<GameTicker>().Respawn(session);
            }
        }
    }

    public TimeSpan? GetRespawnTime(NetUserId user)
    {
        return _respawnInfo.TryGetValue(user, out var data) ? data.RespawnTime : null;
    }

    private ref RespawnData GetRespawnData(NetUserId player)
    {
        if (!_respawnInfo.ContainsKey(player))
            _respawnInfo[player] = new RespawnData();
        return ref CollectionsMarshal.GetValueRefOrNullRef(_respawnInfo, player);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _respawnInfo.Clear();
        _usedNames.Clear();
        _pendingDeathLobby.Clear();
    }
}
