using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Chat.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Radio;
using Content.Server.Speech.Components;
using Content.Server.Speech;
using Content.Shared.Chat;
using Content.Shared.Coordinates;
using Content.Shared._RMC14.Communications;
using Content.Shared._RMC14.Hands;
using Content.Shared._RMC14.Radio;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Telephone;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._SpaceFront.Rules;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Telephone;

public sealed partial class RMCTelephoneSystem : SharedRMCTelephoneSystem
{
    protected override string GetPhoneName(Entity<RotaryPhoneComponent?> phone)
    {
        var name = base.GetPhoneName(phone);

        // Телефон в рюкзаке носит игрок — грид берём от самой сущности телефона (она на корабле/планете).
        var gridUid = Transform(phone.Owner).GridUid ?? EntityUid.Invalid;

        if (EntitySystem.Get<Content.Server._RMC14.Rules.DistressSignal.CMDistressSignalRuleSystem>().ActiveRule is { SquadWar: true } rule)
        {
            var isShuttle = HasComp<Content.Shared._RMC14.Dropship.DropshipComponent>(gridUid);
            var shipTwo = isShuttle
                ? HasComp<Content.Shared._SpaceFront.Rules.SquadWarShipTwoComponent>(gridUid)
                : rule.ShipTwoGrid != null && gridUid == rule.ShipTwoGrid;
            var shipOne = isShuttle
                ? !shipTwo
                : rule.ShipOneGrid != null && gridUid == rule.ShipOneGrid;

            if (shipTwo)
                return $"[АБ] {name}";
            if (shipOne)
                return $"[ДЧ] {name}";
        }

        return name;
    }
}
