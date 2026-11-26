using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Shared.CCVar;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Station.Systems;

public sealed partial class StationJobsSystem : EntitySystem
{
    public void RemoveJob(EntityUid station, string jobPrototypeId, StationJobsComponent? stationJobs = null)
    {
        if (!Resolve(station, ref stationJobs))
            throw new ArgumentException("Tried to use a non-station entity as a station!", nameof(station));

        if (stationJobs.JobList.Remove(jobPrototypeId, out var existing))
            stationJobs.TotalJobs -= existing ?? 0;

        // SquadWar: убираем и из исходного списка, чтобы роль не вернулась при реинициализации станции.
        stationJobs.SetupAvailableJobs.Remove(jobPrototypeId);

        UpdateJobsAvailable();
    }
}
