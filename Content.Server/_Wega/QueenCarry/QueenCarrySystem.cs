using Content.Shared._Wega.QueenCarry;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Verbs;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.Actions;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Players;
using Robust.Shared.Player;
using Content.Shared._RMC14.Xenonids;

namespace Content.Server._Wega.QueenCarry;

public sealed partial class QueenCarrySystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnVerbs);
        SubscribeLocalEvent<HandsComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CarriedMobComponent, DroppedEvent>(OnDropped);
        SubscribeLocalEvent<CarriedMobComponent, GotUnequippedHandEvent>(OnUnequipHand);
    }

    private void OnVerbs(GetVerbsEvent<Verb> args)
    {
        if (args.Target is not { Valid: true } target || !args.CanInteract)
            return;

        if (!HasComp<XenoComponent>(args.User) || !HasComp<HandsComponent>(target) || target == args.User)
            return;

        if (HasComp<CarriedMobComponent>(target))
            return;

        var verb = new Verb
        {
            Category = VerbCategory.Interaction,
            Text = Loc.GetString("queen-carry-name"),
            Act = () => Carry(args.User, target),
            DoContactInteraction = true,
        };

        args.Verbs.Add(verb);
    }

    private void OnInteractHand(Entity<HandsComponent> target, ref InteractHandEvent args)
    {
        if (args.Handled || args.User == target.Owner)
            return;

        if (!HasComp<XenoComponent>(args.User))
            return;

        if (HasComp<CarriedMobComponent>(target.Owner))
            return;

        Carry(args.User, target.Owner);
        args.Handled = true;
    }

    private void Carry(EntityUid queen, EntityUid target)
    {
        if (!HasComp<XenoComponent>(queen))
            return;

        // Брать можно только вплотную и по прямой видимости (не сквозь стены)
        if (!_interaction.InRangeUnobstructed(queen, target))
            return;

        // Ванильная анимация подлёта (видна всем, включая саму королеву)
        var from = Transform(target).Coordinates;
        var to = Transform(queen).Coordinates;
        _storage.PlayPickupAnimation(target, from, to, Transform(target).LocalRotation, null);

        // Временно даём человеку компонент предмета, чтобы королева могла взять его в руку.
        EnsureComp<CarriedMobComponent>(target);
        EnsureComp<ItemComponent>(target);
        _hands.TryPickupAnyHand(queen, target, checkActionBlocker: false, animateUser: true, animate: false);
    }

    // Убираем кнопку тактической карты из хотбара, когда игрок входит в королеву (алерт справа остаётся)
    private void OnPlayerAttached(PlayerAttachedEvent args)
    {
        if (!HasComp<XenoComponent>(args.Entity))
            return;

        if (TryComp<TacticalMapUserComponent>(args.Entity, out var tac) && tac.Action is { } action)
            _actions.RemoveAction(args.Entity, action);
    }

    private void OnDropped(Entity<CarriedMobComponent> ent, ref DroppedEvent args)
    {
        RemComp<ItemComponent>(ent.Owner);
        RemComp<CarriedMobComponent>(ent.Owner);
    }

    private void OnUnequipHand(Entity<CarriedMobComponent> ent, ref GotUnequippedHandEvent args)
    {
        RemComp<ItemComponent>(ent.Owner);
        RemComp<CarriedMobComponent>(ent.Owner);
    }
}
