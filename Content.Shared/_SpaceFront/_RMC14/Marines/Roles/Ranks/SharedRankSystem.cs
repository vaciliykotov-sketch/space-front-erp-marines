using Content.Shared.Dataset;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Enums;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using System.Linq;

using Content.Shared._SpaceFront.Rules;
namespace Content.Shared._RMC14.Marines.Roles.Ranks;

public abstract partial class SharedRankSystem : EntitySystem
{
    private void OnSquadWarTeamExamined(Entity<SquadWarTeamComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        Logger.Info($"SquadWar diag: examine team mark — uid={ent.Owner}, team={ent.Comp.Team}, examiner={args.Examiner}");

        using (args.PushGroup("SquadWarTeam", 2))
        {
            args.PushMarkup(Loc.GetString("rmc-team-examine", ("user", ent.Owner), ("team", ent.Comp.Team)));
        }
    }

    public void SetOverallPlaytimeHours(EntityUid uid, int hours)
    {
        var comp = EnsureComp<RankComponent>(uid);
        comp.OverallPlaytimeHours = hours;
        Dirty(uid, comp);
    }
}
