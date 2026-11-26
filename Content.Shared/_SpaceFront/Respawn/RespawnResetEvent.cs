using Robust.Shared.Serialization;
using Content.Shared._SpaceFront.Respawn;

namespace Content.Shared._SpaceFront.Respawn;

/// <summary>
/// Отправляется клиенту: время, после которого игрок может нажать «Возрождение».
/// Space Front: DeathWindowSeconds &gt; 0 — показать окно смерти с таймером,
/// &lt; 0 — скрыть окно, 0 — поведение по умолчанию.
/// </summary>
[Serializable, NetSerializable]
public sealed class RespawnResetEvent(TimeSpan? time) : EntityEventArgs
{
    public readonly TimeSpan? Time = time;

    public int DeathWindowSeconds;
}
