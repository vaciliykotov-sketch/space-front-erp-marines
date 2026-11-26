using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Rules;
using Robust.Shared.Configuration;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Content.Shared._SpaceFront.Rules;

namespace Content.Shared._RMC14.TacticalMap;

public abstract partial class SharedTacticalMapSystem : EntitySystem
{
    protected virtual void UpdateMapData(Entity<TacticalMapComputerComponent> computer, TacticalMapComponent map)
    {
        var ev = new TacticalMapIncludeXenosEvent();
        RaiseLocalEvent(ref ev);

        // SquadWar: до раскрытия карт блипы вражеской команды скрыты.
        var shipTwo = HasComp<SquadWarShipTwoComponent>(computer.Owner);
        var team = shipTwo ? "АБ" : "ДЧ";
        var marineBlips = FilterSquadWarBlips(map.MarineBlips, team);

        if (ev.Include)
        {
            computer.Comp.Blips = new Dictionary<int, TacticalMapBlip>(marineBlips);
            foreach (var blip in map.XenoBlips)
            {
                computer.Comp.Blips.TryAdd(blip.Key, blip.Value);
            }
        }
        else
        {
            computer.Comp.Blips = marineBlips;
        }

        Dirty(computer);

        var lines = EnsureComp<TacticalMapLinesComponent>(computer);
        lines.MarineLines = shipTwo ? map.MarineLinesShipTwo : map.MarineLines;
        Dirty(computer, lines);
    }

    protected Dictionary<int, TacticalMapBlip> FilterSquadWarBlips(Dictionary<int, TacticalMapBlip> source, string? myTeam)
    {
        if (myTeam == null || SquadWarMapRevealed())
            return new Dictionary<int, TacticalMapBlip>(source);

        var enemyIds = new HashSet<int>();
        var query = EntityQueryEnumerator<SquadWarTeamComponent>();
        while (query.MoveNext(out var uid, out var teamComp))
        {
            if (teamComp.Team != myTeam)
                enemyIds.Add(uid.Id);
        }

        if (enemyIds.Count == 0)
            return new Dictionary<int, TacticalMapBlip>(source);

        var result = new Dictionary<int, TacticalMapBlip>(source);
        foreach (var id in enemyIds)
            result.Remove(id);

        return result;
    }

    private bool SquadWarMapRevealed()
    {
        var query = EntityQueryEnumerator<CMDistressSignalRuleComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.SquadWar && comp.SquadWarRevealed)
                return true;
        }

        return false;
    }

    public void AddPermanentMapBlip(EntityUid mapEnt, Vector2i tilePos, int key)
    {
        var map = EnsureComp<TacticalMapComponent>(mapEnt);
        var icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Interface/map_blips.rsi"), "background");
        var blip = new TacticalMapBlip(tilePos, icon, Color.White, TacticalMapBlipStatus.Alive, icon, false);
        map.MarineBlips[key] = blip;
        map.LastUpdateMarineBlips[key] = blip;
        map.MapDirty = true;
        Dirty(mapEnt, map);
    }

    public void AddPermanentMapLine(EntityUid mapEnt, TacticalMapLine line)
    {
        var map = EnsureComp<TacticalMapComponent>(mapEnt);
        map.MarineLines.Add(line);
        map.MarineLinesShipTwo.Add(line);
        map.MapDirty = true;
        Dirty(mapEnt, map);
    }
}
