using Content.Server.StatusEffectNew;
using Content.Shared.Bed.Sleep;
using Content.Shared.Drowsiness;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared._RMC14.Rules;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared._SpaceFront.Rules;

namespace Content.Server.Drowsiness;

public sealed partial class DrowsinessSystem : SharedDrowsinessSystem
{
    private void OnMarionetteTryingToSleep(Entity<SquadWarTestMarionetteComponent> ent, ref TryingToSleepEvent args)
    {
        // Тестовые марионетки не спят вообще.
        args.Cancelled = true;
    }
}
