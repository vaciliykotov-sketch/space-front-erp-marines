using Robust.Shared.Serialization;
using Content.Shared._SpaceFront.Respawn;

namespace Content.Shared._SpaceFront.Respawn;

/// <summary>
/// Space Front: игрок нажал «Уйти сразу в лобби!» в окне смерти.
/// </summary>
[Serializable, NetSerializable]
public sealed class SquadWarLeaveNowEvent : EntityEventArgs
{
}
