using Content.Shared._Wega.ToysLeash;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client._Wega.ToysLeash;

public sealed class ToysColorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Log.Info("SF diag: ToysColorSystem registered");
        // ComponentHandleState занят автогенерацией AutoGenerateComponentState — используем AfterAutoHandleStateEvent.
        SubscribeLocalEvent<ToysColorComponent, AfterAutoHandleStateEvent>(OnAfterState);
        SubscribeLocalEvent<ToysColorComponent, ComponentStartup>(OnStartup);
    }

    private void OnAfterState(EntityUid uid, ToysColorComponent comp, ref AfterAutoHandleStateEvent args)
    {
        Log.Info($"SF diag: toys color afterstate — uid={uid}, color={comp.Color}");
        Apply(uid, comp);
    }

    private void OnStartup(EntityUid uid, ToysColorComponent comp, ComponentStartup args)
    {
        Log.Info($"SF diag: toys color startup — uid={uid}, color={comp.Color}");
        Apply(uid, comp);
    }

    private void Apply(EntityUid uid, ToysColorComponent comp)
    {
        if (TryComp<SpriteComponent>(uid, out var sprite))
        {
            sprite.Color = comp.Color;
            Log.Info($"SF diag: toys color apply — uid={uid}, color={comp.Color}, sprite={sprite.Color}");
        }
    }
}
