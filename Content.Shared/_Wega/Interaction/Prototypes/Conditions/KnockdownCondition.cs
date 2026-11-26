using Content.Shared.Standing;

namespace Content.Shared.Interaction;

[Serializable]
[DataDefinition]
public sealed partial class KnockdownCondition : InteractionCondition
{
    public override bool Check(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        return entityManager.TryGetComponent<StandingStateComponent>(target, out var standing) && !standing.Standing;
    }
}
