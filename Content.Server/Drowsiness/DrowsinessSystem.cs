using Content.Server.StatusEffectNew;
using Content.Shared.Bed.Sleep;
using Content.Shared.Drowsiness;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

// Stories-start: дополнительные using
using Content.Shared._RMC14.Rules;
using Content.Shared._SpaceFront.Rules;
// Stories-end

namespace Content.Server.Drowsiness;

// Stories-start: класс сделан partial для выноса в _SpaceFront
public sealed partial class DrowsinessSystem : SharedDrowsinessSystem
// Stories-end: класс сделан partial для выноса в _SpaceFront
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    /// <inheritdoc/>
    // Stories-start: изменённый член (наша версия, см. PATCHES.md)
    public override void Initialize()
    {
        SubscribeLocalEvent<DrowsinessStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<SquadWarTestMarionetteComponent, TryingToSleepEvent>(OnMarionetteTryingToSleep);
    }
    // Stories-end

    private void OnEffectApplied(Entity<DrowsinessStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        ent.Comp.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(ent.Comp.TimeBetweenIncidents.X, ent.Comp.TimeBetweenIncidents.Y));
    }

    // Stories-start: изменённый член (наша версия, см. PATCHES.md)
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DrowsinessStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out var drowsiness, out var statusEffect))
        {
            if (_timing.CurTime < drowsiness.NextIncidentTime)
                continue;

            if (statusEffect.AppliedTo is null)
                continue;

            // Тестовые марионетки SquadWar не засыпают.
            if (HasComp<SquadWarTestMarionetteComponent>(statusEffect.AppliedTo.Value))
                continue;

            // Set the new time.
            drowsiness.NextIncidentTime = _timing.CurTime + TimeSpan.FromSeconds(_random.NextFloat(drowsiness.TimeBetweenIncidents.X, drowsiness.TimeBetweenIncidents.Y));

            // sleep duration
            var duration = TimeSpan.FromSeconds(_random.NextFloat(drowsiness.DurationOfIncident.X, drowsiness.DurationOfIncident.Y));

            // Make sure the sleep time doesn't cut into the time to next incident.
            drowsiness.NextIncidentTime += duration;

            _statusEffects.TryAddStatusEffectDuration(statusEffect.AppliedTo.Value, SleepingSystem.StatusEffectForcedSleeping, duration);
        }
    }
    // Stories-end
}
