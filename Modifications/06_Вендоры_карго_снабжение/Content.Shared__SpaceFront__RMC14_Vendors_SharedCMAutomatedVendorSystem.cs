using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Access;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Content.Shared.Wall;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged;
using Content.Shared._RMC14.Animations;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared._RMC14.Cassette;
using Content.Shared._RMC14.Cryostorage;
using Content.Shared._RMC14.Holiday;
using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Medical.IV;
using Content.Shared._RMC14.Medical.Refill;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Scaling;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared._RMC14.Tools;
using Content.Shared._RMC14.Weapons.Ranged.Ammo.BulletBox;
using Content.Shared._RMC14.Weapons.Ranged.Chamber;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared._RMC14.Webbing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Linq;
using System.Numerics;

namespace Content.Shared._RMC14.Vendors;

public abstract partial class SharedCMAutomatedVendorSystem : EntitySystem
{
    public void GrantSquadWarVendorPoints(EntityUid uid)
    {
        var user = EnsureComp<CMVendorUserComponent>(uid);
        if (user.Points < 45)
            SetPoints((uid, user), 45);

        // Очки для вендоров со своим типом (Specialist, Chemicals и т.п.).
        foreach (var type in new[] { "Specialist", "ExperimentalTools", "AssaultEngineer", "Chemicals" })
        {
            user.ExtraPoints ??= new Dictionary<string, int>();
            if (user.ExtraPoints.GetValueOrDefault(type) < 45)
                user.ExtraPoints[type] = 45;
        }

        Dirty(uid, user);
    }
}
