using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Flash;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Popups;
using Content.Shared.Prototypes;
using Content.Shared.Storage;
using Content.Shared.Throwing;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;

using Content.Shared._SpaceFront.Marines;
namespace Content.Shared._RMC14.Marines.Skills;

public sealed partial class SkillsSystem : EntitySystem
{
    private void OnSkillGrantOnUse(Entity<SkillGrantOnUseComponent> ent, ref UseInHandEvent args)
    {
        var user = args.User;
        var skills = EnsureComp<SkillsComponent>(user);

        foreach (var (skill, level) in ent.Comp.Skills)
        {
            if (!skills.Skills.TryGetValue(skill, out var current) || current < level)
                skills.Skills[skill] = level;
        }

        Dirty(user, skills);
        _popup.PopupEntity(Loc.GetString("rmc-skill-grant-on-use"), user, user);
    }

    public void GrantMaxSkills(EntityUid uid)
    {
        var skills = EnsureComp<SkillsComponent>(uid);
        foreach (var skill in Skills)
        {
            skills.Skills[skill] = 3;
        }

        Dirty(uid, skills);
    }
}
