using Content.Shared.Standing;

namespace Content.Shared.Interaction;

[Serializable]
[DataDefinition]
public sealed partial class StandEffect : InteractionEffect
{
    public override void Apply(EntityUid user, EntityUid target, IEntityManager entityManager)
    {
        entityManager.System<StandingStateSystem>().Stand(target);
    }
}
