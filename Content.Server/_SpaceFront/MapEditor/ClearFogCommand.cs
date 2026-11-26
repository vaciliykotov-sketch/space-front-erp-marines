using Content.Server._RMC14.Rules.DistressSignal;
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._SpaceFront.MapEditor;

/// <summary>
/// SquadWar: рассеять туман немедленно (без ожидания таймера).
/// </summary>
[AdminCommand(AdminFlags.Spawn)]
public sealed class ClearFogCommand : IConsoleCommand
{
    public string Command => "clearfog";
    public string Description => "SquadWar: рассеять туман.";
    public string Help => "clearfog";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var ruleSys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<CMDistressSignalRuleSystem>();
        var rule = ruleSys.ActiveRule;
        if (rule == null)
        {
            shell.WriteLine("Нет активного правила SquadWar.");
            return;
        }

        ruleSys.ClearSquadWarFog(rule);
        shell.WriteLine("Туман рассеян.");
    }
}
