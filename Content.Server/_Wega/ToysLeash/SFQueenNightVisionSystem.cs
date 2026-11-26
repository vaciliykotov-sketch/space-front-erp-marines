using Content.Shared._RMC14.NightVision;
using Content.Shared._Wega.ToysLeash;
using Content.Server._RMC14.NightVision;
using Robust.Server.GameObjects;

namespace Content.Server._Wega.ToysLeash;

public sealed class SFQueenNightVisionSystem : EntitySystem
{
    [Dependency] private readonly NightVisionSystem _nightVision = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NightVisionComponent, SFXenoNightVisionActionEvent>(OnToggle);
    }

    private void OnToggle(Entity<NightVisionComponent> ent, ref SFXenoNightVisionActionEvent args)
    {
        _nightVision.Toggle((ent.Owner, (NightVisionComponent?)ent.Comp));
    }
}
