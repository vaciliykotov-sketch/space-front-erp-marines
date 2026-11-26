using System.Linq;
using System.Text.Json.Nodes;
using Content.Server._RMC14.Rules.DistressSignal;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Server.ServerStatus;
using Robust.Shared.Configuration;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        /// <summary>
        ///     Used for thread safety, given <see cref="IStatusHost.OnStatusRequest"/> is called from another thread.
        /// </summary>
        private readonly object _statusShellLock = new();

        /// <summary>
        ///     Round start time in UTC, for status shell purposes.
        /// </summary>
        [ViewVariables]
        private DateTime _roundStartDateTime;

        /// <summary>
        ///     For access to CVars in status responses.
        /// </summary>
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        /// <summary>
        ///     For access to the round ID in status responses.
        /// </summary>
        [Dependency] private readonly SharedGameTicker _gameTicker = default!;
        /// <summary>
        ///     Needed to get entity system instances.
        /// </summary>
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

        private void InitializeStatusShell()
        {
            IoCManager.Resolve<IStatusHost>().OnStatusRequest += GetStatusResponse;
        }

        // Stories-start: изменённый член (наша версия, см. PATCHES.md)
        private void GetStatusResponse(JsonNode jObject)
        {
            var preset = CurrentPreset ?? Preset;
            var cmDistressSignalRuleSystem = _entitySystemManager.GetEntitySystem<CMDistressSignalRuleSystem>();

            // This method is raised from another thread, so this better be thread safe!
            lock (_statusShellLock)
            {
                // Space Front: статичное имя со стрелкой; таймер показывается
                // в поле времени лаунчера (см. ниже).
                jObject["name"] = _baseServer.ServerName + " /// До начала игры осталось ➜";
                // Space Front: теги для лаунчера — 18+ (ERP), язык, регион.
                jObject["tags"] = new JsonArray("18+", "lang:ru", "region:eu_e");
                jObject["map"] = _gameMapManager.GetSelectedMap()?.MapName;
                jObject["round_id"] = _gameTicker.RoundId;
                // Space Front: счётчик игроков в лаунчере х2.
                var reportedPlayers = _cfg.GetCVar(CCVars.AdminsCountInReportedPlayerCount)
                    ? _playerManager.PlayerCount
                    : _playerManager.PlayerCount - _adminManager.ActiveAdmins.Count();
                jObject["players"] = reportedPlayers * 2;
                jObject["soft_max_players"] = _cfg.GetCVar(CCVars.SoftMaxPlayers);
                jObject["panic_bunker"] = _cfg.GetCVar(CCVars.PanicBunkerEnabled);
                jObject["run_level"] = (int) _runLevel;
                if (preset != null)
                    jObject["preset"] = Loc.GetString(preset.ModeTitle);

                var planetMapName = cmDistressSignalRuleSystem.SelectedPlanetMapName;
                if (!string.IsNullOrEmpty(planetMapName))
                    jObject["planet_map"] = planetMapName;

                try
                {
                    var msk = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                    var now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, msk);
                    var today17 = now.Date.AddHours(17);
                    var today21 = now.Date.AddHours(21);

                    if (_runLevel >= GameRunLevel.InRound || (now >= today17 && now < today21))
                    {
                        // Space Front: во время раунда и в игровом окне 17:00–21:00
                        // в поле времени ВСЕГДА «Лобби».
                        jObject["run_level"] = (int)GameRunLevel.PreRoundLobby;
                    }
                    else
                    {
                        // Space Front: в лобби вне игрового окна — обратный отсчёт до сбора.
                        var remaining = now >= today21 ? today17.AddDays(1) - now : today17 - now;
                        jObject["run_level"] = (int)GameRunLevel.InRound;
                        jObject["round_start_time"] = DateTime.UtcNow.Subtract(remaining).ToString("o");
                    }
                }
                catch
                {
                    // при любой ошибке — как ваниль
                }
            }
        }
        // Stories-end
    }
}
