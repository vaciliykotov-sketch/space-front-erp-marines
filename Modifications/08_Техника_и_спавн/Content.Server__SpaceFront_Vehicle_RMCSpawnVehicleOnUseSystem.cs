using Content.Shared._SpaceFront.Vehicle;
using Content.Shared.Interaction.Events;
using Content.Server._SpaceFront.Vehicle;

namespace Content.Server._SpaceFront.Vehicle;

public sealed class RMCSpawnVehicleOnUseSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RMCSpawnVehicleOnUseComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<RMCSpawnVehicleOnUseComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var coords = Transform(args.User).Coordinates;
        Spawn(ent.Comp.Vehicle, coords);
        QueueDel(ent);
    }
}
