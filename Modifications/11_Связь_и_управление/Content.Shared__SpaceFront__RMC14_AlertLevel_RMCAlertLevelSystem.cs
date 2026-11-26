using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Ghost;
using Content.Shared.Lock;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.ARES;
using Content.Shared._RMC14.Doors;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Marines;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.AlertLevel;

public sealed partial class RMCAlertLevelSystem : EntitySystem
{
    public bool IsRedOrDeltaAlert(EntityUid? map = null)
    {
        var level = map != null ? Get(map.Value) : Get();
        return level is RMCAlertLevels.Red or RMCAlertLevels.Delta;
    }

    public RMCAlertLevels? Get(EntityUid map)
    {
        if (!TryGetAlertLevel(out var alert))
            return null;

        if (alert.Comp.MapLevels.TryGetValue(map, out var level))
            return level;

        return alert.Comp.Level;
    }

    public void Set(RMCAlertLevels level, EntityUid? user, bool playSound = true, bool sendAnnouncement = true, EntityUid? map = null)
    {
        var ent = EnsureAlertLevel();

        // SquadWar: локальный уровень по карте — красный код только на одном Алмаере.
        if (map != null)
        {
            var prev = ent.Comp.MapLevels.TryGetValue(map.Value, out var prevLevel)
                ? prevLevel
                : ent.Comp.Level;

            ent.Comp.MapLevels[map.Value] = level;
            Dirty(ent);

            _adminLog.Add(LogType.RMCAlertLevel, $"{ToPrettyString(user)} set alert level on {ToPrettyString(map.Value)} to {level}");

            ApplyAlertLevel(ent, level, prev, user, playSound, sendAnnouncement, map);
            return;
        }

        if (ent.Comp.Level == level)
            return;

        var prevGlobal = ent.Comp.Level;
        ent.Comp.Level = level;
        Dirty(ent);

        _adminLog.Add(LogType.RMCAlertLevel, $"{ToPrettyString(user)} set alert level to {level}");

        ApplyAlertLevel(ent, level, prevGlobal, user, playSound, sendAnnouncement, null);
    }

    private void ApplyAlertLevel(Entity<RMCAlertLevelComponent> ent, RMCAlertLevels level, RMCAlertLevels prev, EntityUid? user, bool playSound, bool sendAnnouncement, EntityUid? map)
    {
        var (sound, message, announcement) = level switch
        {
            RMCAlertLevels.Green => (ent.Comp.GreenSound, ent.Comp.GreenMessage, null),
            RMCAlertLevels.Blue when prev < RMCAlertLevels.Blue => (ent.Comp.BlueElevatedSound, ent.Comp.BlueElevatedMessage, null),
            RMCAlertLevels.Blue when prev > RMCAlertLevels.Blue => (ent.Comp.BlueLoweredSound, ent.Comp.BlueLoweredMessage, null),
            RMCAlertLevels.Red when prev < RMCAlertLevels.Red => (ent.Comp.RedElevatedSound, ent.Comp.RedElevatedMessage, null),
            RMCAlertLevels.Red when prev > RMCAlertLevels.Red => (ent.Comp.RedLoweredSound, ent.Comp.RedLoweredMessage, null),
            RMCAlertLevels.Delta => (ent.Comp.DeltaSound, ent.Comp.DeltaAnnouncement, ent.Comp.DeltaAnnouncement),
            _ => (null, null, null),
        };

        var almayers = new HashSet<EntityUid>();
        var almayerQuery = EntityQueryEnumerator<AlmayerComponent>();
        while (almayerQuery.MoveNext(out var uid, out _))
        {
            almayers.Add(uid);
        }

        if (user != null && map == null)
        {
            foreach (var almayer in almayers)
            {
                _aresCore.CreateARESLog(almayer, LogCat, (string)$"{Name(user.Value)} set the alert level to: {level}");
            }
        }

        var transformQuery = EntityManager.TransformQuery;
        var filter = Filter.Empty()
            .AddWhereAttachedEntity(entity =>
            {
                if (_ghostQuery.HasComp(entity))
                    return true;

                if (transformQuery.CompOrNull(entity)?.MapUid is not { } mapId)
                    return false;

                return map != null ? mapId == map : almayers.Contains(mapId);
            });

        // Play alarm sound if playSound == true
        if (playSound && _net.IsServer)
        {
            _audio.PlayGlobal(sound, filter, true);
        }

        // Send announcement if sendAnnouncement == true
        if (sendAnnouncement)
        {
            if (announcement != null)
            {
                _marineAnnounce.AnnounceToMarines(Loc.GetString(announcement));
            }
            else if (message != null)
            {
                var ares = _aresCore.EnsureMarineARES();
                _marineAnnounce.AnnounceRadio(ares, Loc.GetString(message.Value), ent.Comp.RadioChannel);
            }
        }

        var unlockQuery = EntityQueryEnumerator<RMCUnlockOnAlertLevelComponent, LockComponent, TransformComponent>();
        while (unlockQuery.MoveNext(out var uid, out var unlock, out var lockComp, out var xform))
        {
            if (map != null && xform.MapUid != map)
                continue;

            if (unlock.Level <= level)
            {
                _lock.Unlock(uid, null, lockComp);
            }
            else
            {
                SharedEntityStorageComponent? entityStorageComp = null;
                if (_entityStorage.ResolveStorage(uid, ref entityStorageComp))
                    _entityStorage.CloseStorage(uid, entityStorageComp); // Close a locker before locking it.
                _lock.Lock(uid, null, lockComp);
            }
        }

        var openQuery = EntityQueryEnumerator<RMCOpenOnAlertLevelComponent, DoorComponent, RMCPodDoorComponent, TransformComponent>();
        while (openQuery.MoveNext(out var uid, out var unlock, out var door, out var podDoor, out var xform))
        {
            if (map != null && xform.MapUid != map)
                continue;

            if (unlock.Id != podDoor.Id)
                continue;

            if (unlock.Level <= level)
                _door.TryOpen(uid, door);
            else
                _door.TryClose(uid, door);
        }

        var displays = EntityQueryEnumerator<RMCAlertLevelDisplayComponent, TransformComponent>();
        while (displays.MoveNext(out var uid, out _, out var xform))
        {
            if (map != null && xform.MapUid != map)
                continue;

            _appearance.SetData(uid, RMCAlertLevelsVisuals.Alert, level);
        }

        var ev = new RMCAlertLevelChangedEvent(level);
        RaiseLocalEvent(ent, ref ev, true);
    }
}
