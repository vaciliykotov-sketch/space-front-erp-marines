using Content.Server.Announcements;
using Content.Server.Discord;
using Content.Server.GameTicking.Events;
using Content.Server.Ghost;
using Content.Server.Maps;
using Content.Server.Roles;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Mind;
using Content.Shared.Players;
using Content.Shared.Preferences;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Prototypes;
using JetBrains.Annotations;
using Prometheus;
using Robust.Shared.Asynchronous;
using Robust.Shared.Audio;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map.Components;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;

namespace Content.Server.GameTicking;

    public sealed partial class GameTicker
{
        private void SetLobbyDurationForSchedule()
        {
            TimeSpan delay;
            try
            {
                var msk = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");
                var now = TimeZoneInfo.ConvertTime(DateTime.Now, msk);
                var today17 = now.Date.AddHours(17);
                if (now < today17)
                    delay = today17 - now;
                else if (now >= now.Date.AddHours(21))
                    delay = today17.AddDays(1) - now;
                else
                    delay = TimeSpan.FromSeconds(180);
            }
            catch
            {
                delay = TimeSpan.FromSeconds(180);
            }

            _cfg.SetCVar("game.lobbyduration", (int)delay.TotalSeconds);
        }

        private TimeSpan _nextLobbyScheduleSync;
}
