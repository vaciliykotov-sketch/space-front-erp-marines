using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Audio;
using Content.Shared.Database;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Xenonids;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Telephone;

public abstract partial class SharedRMCTelephoneSystem : EntitySystem
{
    protected virtual string GetPhoneName(Entity<RotaryPhoneComponent?> phone)
    {
        var name = Name(phone);
        if (!Resolve(phone, ref phone.Comp, false))
            return name;

        if (!phone.Comp.TryGetHolderName)
            return name;

        if (!TryGetPhoneBackpackHolder(phone, out var holder))
            return name;

        name = Name(holder);
        if (TryComp(holder, out JobPrefixComponent? jobPrefix))
            name = $"{Loc.GetString(jobPrefix.Prefix)} {name}";

        if (_squad.TryGetMemberSquad(holder, out var squad))
            name = $"{name} ({Name(squad)})";

        return name;
    }
}
