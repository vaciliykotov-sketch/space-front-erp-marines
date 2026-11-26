using System.Net;
using Content.Server.Database;
using Content.Shared.CCVar;
using Content.Shared.Info;
using Robust.Shared.Configuration;
using Robust.Shared.Network;

namespace Content.Server.Info;

// Stories-start: класс сделан partial для выноса в _SpaceFront
public sealed partial class RulesManager
// Stories-end: класс сделан partial для выноса в _SpaceFront
{
    [Dependency] private readonly IServerDbManager _dbManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private static DateTime LastValidReadTime => DateTime.UtcNow - TimeSpan.FromDays(60);

    public void Initialize()
    {
        _netManager.Connected += OnConnected;
        _netManager.RegisterNetMessage<SendRulesInformationMessage>();
        _netManager.RegisterNetMessage<RulesAcceptedMessage>(OnRulesAccepted);
    }

    // Stories-start: изменённый член (наша версия, см. PATCHES.md)
    private async void OnConnected(object? sender, NetChannelArgs e)
    {
         var isLocalhost = IPAddress.IsLoopback(e.Channel.RemoteEndPoint.Address) &&
                               _cfg.GetCVar(CCVars.RulesExemptLocal);

        // Правила показываются при каждом заходе на сервер.
        // (Запись о прочтении всё равно обновляется в БД для статистики.)
        var showRulesMessage = new SendRulesInformationMessage
        {
            PopupTime = _cfg.GetCVar(CCVars.RulesWaitTime),
            CoreRules = _cfg.GetCVar(CCVars.RulesFile),
            ShouldShowRules = !isLocalhost,
        };
        _netManager.ServerSendMessage(showRulesMessage, e.Channel);
    }
    // Stories-end

    private async void OnRulesAccepted(RulesAcceptedMessage message)
    {
        var date = DateTime.UtcNow;
        await _dbManager.SetLastReadRules(message.MsgChannel.UserId, date);
    }
}
