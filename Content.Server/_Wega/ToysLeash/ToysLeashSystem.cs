using Content.Shared._Wega.ToysLeash;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Humanoid;
using Content.Shared.Sprite;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Server.Preferences;
using Content.Server.Preferences.Managers;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared.Preferences;
using Robust.Server.Player;
using Robust.Shared.Maths;
using Robust.Shared.Map;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Spawners;
using Content.Shared._RMC14.Rules;
using Content.Shared._SpaceFront.TTS;
using Robust.Shared.Timing;
using Robust.Shared.Containers;

using System.Collections.Generic;
using Robust.Shared.Random;
using Content.Shared.Actions;
using Content.Shared._RMC14.TacticalMap;

namespace Content.Server._Wega.ToysLeash;

public sealed partial class ToysLeashSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    private bool _queenSpawned;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const string DildoPrototype = "RandomDildo";
    private const string LeftHand = "Left";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnVerbs);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<PullableComponent, PullStoppedMessage>(OnPullStoppedComp);
        SubscribeLocalEvent<ToysBoundComponent, GotEquippedEvent>(OnToyEquipped);
        SubscribeLocalEvent<ToysBoundComponent, GotUnequippedEvent>(OnToyUnequipped);
    }

    private void OnVerbs(GetVerbsEvent<Verb> args)
    {
        if (args.Target is not { Valid: true } target || !HasComp<InteractionActionsComponent>(target))
            return;

        if (args.User != target || !args.CanInteract)
            return;

        // Space Front: женщины и ксено не достают прибор.
        if (HasComp<XenoComponent>(args.User) ||
            (TryComp<HumanoidAppearanceComponent>(args.User, out var ha) && ha.Sex == Sex.Female))
            return;

        var hasToy = FindBoundToy(args.User, out _);

        var verb = new Verb
        {
            Category = VerbCategory.Interaction,
            Text = Loc.GetString(hasToy ? "toys-put-away-name" : "toys-take-out-name"),
            Impact = LogImpact.Medium,
            DoContactInteraction = false,
            Act = () =>
            {
                if (hasToy)
                    PutAway(args.User);
                else
                    TakeOut(args.User);
            },
        };

        args.Verbs.Add(verb);
    }

    // Прибор может быть в руках или в слоте игрушек.
    private bool FindBoundToy(EntityUid user, out EntityUid toy)
    {
        toy = default;

        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (HasComp<ToysBoundComponent>(held))
            {
                toy = held;
                return true;
            }
        }

        if ((_inventory.TryGetSlotEntity(user, "toys", out var inSlot) || _inventory.TryGetSlotEntity(user, "toys2", out inSlot)) && HasComp<ToysBoundComponent>(inSlot.Value))
        {
            toy = inSlot.Value;
            return true;
        }

        return false;
    }

    // Королева спавнится на планете в начале каждого раунда (гост-роль — за неё может зайти любой).
    private void OnRoundStarting(RoundStartingEvent ev)
    {
        // Планета грузится чуть позже старта раунда — ждём 3 секунды.
        Timer.Spawn(TimeSpan.FromSeconds(3), () =>
        {
            if (_queenSpawned)
                return;

            TrySpawnQueenAtLz();
        });
    }

    private void TrySpawnQueenAtLz()
    {
        if (_queenSpawned)
            return;

        // Рандомная зона посадки (ЗП): таблички CMSemioticLandingzone на планете.
        var lz = new List<EntityUid>();
        var metaQuery = AllEntityQuery<MetaDataComponent, TransformComponent>();
        while (metaQuery.MoveNext(out var metaUid, out var meta, out var xform))
        {
            if (meta.EntityPrototype?.ID == "CMSemioticLandingzone")
                lz.Add(metaUid);
        }

        if (lz.Count > 0)
        {
            var pick = lz[_random.Next(lz.Count)];
            SpawnQueen(Transform(pick).Coordinates);
            _queenSpawned = true;
            Log.Info($"SF diag: queen spawned at LZ mark {pick} of {lz.Count}");
            return;
        }

        // Фоллбэк: ксено-спавнпоинт или центр планеты.
        var spawnQuery = AllEntityQuery<XenoSpawnPointComponent>();
        while (spawnQuery.MoveNext(out var spawnUid, out _))
        {
            SpawnQueen(Transform(spawnUid).Coordinates);
            _queenSpawned = true;
            return;
        }

        var planetQuery = AllEntityQuery<RMCPlanetComponent>();
        while (planetQuery.MoveNext(out var planetUid, out _))
        {
            SpawnQueen(Transform(planetUid).Coordinates);
            _queenSpawned = true;
            return;
        }

        Log.Info("SF diag: queen spawn skipped — no planet found");
    }

    private void SpawnQueen(EntityCoordinates coords)
    {
        var queen = Spawn("CMXenoQueen", coords);
        if (TryComp<TTSComponent>(queen, out var tts))
        {
            tts.VoicePrototypeId = "phantom_assassin_arcana_dota_2";
            Dirty(queen, tts);
        }
        Log.Info($"SF diag: queen spawned — uid={queen}");
    }

    private void TakeOut(EntityUid user)
    {
        var item = Spawn(DildoPrototype, Transform(user).Coordinates);
        EnsureComp<ToysBoundComponent>(item).Owner = user;
        EnsureComp<UnremoveableComponent>(item);

        // Цвет прибора повторяет цвет кожи хозяина (RandomSprite применяет selected на клиенте).
        if (TryComp<HumanoidAppearanceComponent>(user, out var humanoid))
        {
            // Цвет прибора: градиент «бежевый → шоколадный» по светлоте кожи.
            var skinColor = humanoid.SkinColor;
            if (_playerManager.TryGetSessionByEntity(user, out var session))
            {
                try
                {
                    if (_prefs.GetPreferences(session.UserId).SelectedCharacter is HumanoidCharacterProfile prof)
                        skinColor = prof.Appearance.SkinColor;
                }
                catch
                {
                    // оставляем цвет из компонента
                }
            }

            var hsv = Color.ToHsv(skinColor);
            // 0 — очень светлая кожа (бежевый), 1 — тёмная (шоколадный).
            var t = Math.Clamp((0.9f - hsv.Z) / 0.5f, 0f, 1f);
            var beige = Color.FromHex("#E8C49C");
            var chocolate = Color.FromHex("#6B4226");
            var toysColor = EnsureComp<ToysColorComponent>(item);
            toysColor.Color = Color.InterpolateBetween(beige, chocolate, t);
            Dirty(item, toysColor);
            Log.Info($"SF diag: toy color — skin={skinColor}, hsvZ={hsv.Z:F2}, t={t:F2}, final={toysColor.Color}");
        }

        var pickedLeft = _hands.TryPickup(user, item, LeftHand, checkActionBlocker: false);
        var pickedAny = !pickedLeft && _hands.TryPickupAnyHand(user, item, checkActionBlocker: false);

        if (!pickedLeft && !pickedAny)
        {
            Log.Info($"SF diag: takeout failed — left={pickedLeft}, any={pickedAny}, user={ToPrettyString(user)}");
            Del(item);
            _popup.PopupEntity(Loc.GetString("toys-hands-full"), user, user, PopupType.SmallCaution);
            return;
        }

        Log.Info($"SF diag: takeout ok — left={pickedLeft}, any={pickedAny}");

        _popup.PopupEntity(Loc.GetString("toys-taken-out"), user, user, PopupType.Small);
    }

    // Прибор положили в чужой спец-карман — владелец берёт носителя на поводок.
    // Если тянуть нельзя (труп/ксено) — прибор сразу возвращается владельцу.
    private void OnToyEquipped(Entity<ToysBoundComponent> ent, ref GotEquippedEvent args)
    {
        if (ent.Comp.Owner is not { Valid: true } owner || owner == args.Equipee)
            return;

        if (!_pulling.TryStartPull(owner, args.Equipee))
        {
            Timer.Spawn(TimeSpan.FromMilliseconds(300), () =>
            {
                if (!Exists(ent.Owner))
                    return;
                ReturnToyToOwner(ent.Owner);
            });
        }
    }

    private void ReturnToyToOwner(EntityUid toyUid)
    {
        if (!TryComp<ToysBoundComponent>(toyUid, out var bound) || bound.Owner is not { Valid: true } owner)
            return;

        var slotOwner = Transform(toyUid).ParentUid;

        // Вынимаем только если в слоте лежит именно прибор (а не, например, человек на руках).
        var slot = "";
        if (_container.TryGetContainer(slotOwner, "toys", out var toysCont) && toysCont.Contains(toyUid))
            slot = "toys";
        else if (_container.TryGetContainer(slotOwner, "toys2", out var toys2Cont) && toys2Cont.Contains(toyUid))
            slot = "toys2";

        if (slot == "")
        {
            EnsureComp<UnremoveableComponent>(toyUid);
            return;
        }

        RemComp<UnremoveableComponent>(toyUid);
        if (!_inventory.TryUnequip(slotOwner, slot, out var item, checkDoafter: false))
        {
            EnsureComp<UnremoveableComponent>(toyUid);
            return;
        }
        if (!_hands.TryPickupAnyHand(owner, item.Value, checkActionBlocker: false))
        {
            if (TryComp<HandsComponent>(owner, out var handsComp) && handsComp.ActiveHandId is { } handId &&
                _container.TryGetContainer(owner, handId, out var handContainer))
            {
                _container.Insert(item.Value, handContainer, force: true);
            }
        }
        EnsureComp<UnremoveableComponent>(toyUid);
        ReleaseHandBlock(toyUid);
        _inventory.ShowToysReturnEffects(owner, slotOwner);
    }

    // Прибор покинул слот — разблокируем руку того, кто его туда клал.
    private void OnToyUnequipped(Entity<ToysBoundComponent> ent, ref GotUnequippedEvent args)
    {
        ReleaseHandBlock(ent.Owner);
    }

    private void ReleaseHandBlock(EntityUid toyUid)
    {
        var query = EntityQueryEnumerator<ToysHandBlockComponent>();
        while (query.MoveNext(out var virtUid, out var block))
        {
            if (block.ToyUid == toyUid)
                Del(virtUid);
        }
    }

    private void PutAway(EntityUid user)
    {
        if (!FindBoundToy(user, out var toy))
            return;

        Del(toy);
        _popup.PopupEntity(Loc.GetString("toys-put-away"), user, user, PopupType.Small);
    }

    // Цепь разорвалась — прибор возвращается владельцу в левую руку.
    private void OnPullStoppedComp(Entity<PullableComponent> ent, ref PullStoppedMessage ev)
    {
        if (ent.Owner != ev.PullerUid)
            return;

        var puller = ev.PullerUid;
        var pulled = ev.PulledUid;

        if (!Exists(puller) || !Exists(pulled))
            return;

        var toySlot = _inventory.TryGetSlotEntity(pulled, "toys", out var toy) ? "toys" : "toys2";
        if ((toySlot == "toys2" && !_inventory.TryGetSlotEntity(pulled, "toys2", out toy)) ||
            !TryComp<ToysBoundComponent>(toy!.Value, out var bound) || bound.Owner != puller)
            return;

        ReleaseHandBlock(toy.Value);
        RemComp<UnremoveableComponent>(toy.Value);
        if (_inventory.TryUnequip(pulled, toySlot, out var item, checkDoafter: false))
        {
            if (!_hands.TryPickupAnyHand(puller, item.Value, checkActionBlocker: false))
            {
                // Обе руки заняты — силой кладём в активную руку, вытесняя старый предмет.
                if (TryComp<HandsComponent>(puller, out var handsComp) && handsComp.ActiveHandId is { } handId &&
                    _container.TryGetContainer(puller, handId, out var handContainer))
                {
                    _container.Insert(item.Value, handContainer, force: true);
                }
            }
        }
        EnsureComp<UnremoveableComponent>(toy.Value);
        _inventory.ShowToysReturnEffects(puller, pulled);
    }
}
