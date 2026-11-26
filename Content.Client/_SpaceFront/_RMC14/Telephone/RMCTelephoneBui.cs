using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._RMC14.Telephone;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RMC14.Telephone;

public sealed partial class RMCTelephoneBui : BoundUserInterface
{
    private static readonly List<string> TabOrder = new() { "Военная полиция", "Алмаер", "Командование", "Кабинеты", "АРЭС", "Шаттлы", "Морпехи" };
}
