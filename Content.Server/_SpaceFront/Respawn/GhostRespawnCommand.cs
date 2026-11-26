using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Shared.Administration;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Content.Server._SpaceFront.Respawn;

/// <summary>
/// Возвращает призрака в лобби, если прошло достаточно времени после смерти.
/// Кнопка в интерфейсе призрака выполняет именно эту команду.
/// </summary>
[AnyCommand]
public sealed class GhostRespawnCommand : IConsoleCommand
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;

    public string Command => "ghostrespawn";
    public string Description => "Возвращает призрака в лобби, если прошло достаточно времени после смерти.";
    public string Help => "ghostrespawn";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is null)
        {
            shell.WriteLine("Недоступно из серверной консоли.");
            return;
        }

        if (shell.Player.AttachedEntity is null)
        {
            shell.WriteLine("Недоступно в лобби или без сущности.");
            return;
        }

        if (!_entityManager.TryGetComponent<GhostComponent>(shell.Player.AttachedEntity, out _))
        {
            shell.WriteLine("Вы не призрак.");
            return;
        }

        var respawnSystem = _entitySystemManager.GetEntitySystem<RespawnSystem>();
        var respawnTime = respawnSystem.GetRespawnTime(shell.Player.UserId);

        // Страховка: если смерть не зафиксирована — запускаем таймер сейчас и отказываем.
        if (respawnTime is null)
        {
            respawnSystem.OnPlayerDied(shell.Player.UserId);
            IoCManager.Resolve<IChatManager>().DispatchServerMessage(shell.Player,
                "Таймер возрождения запущен. Подождите 10 минут.");
            return;
        }

        if (_gameTiming.CurTime < respawnTime.Value)
        {
            var timeLeft = (respawnTime.Value - _gameTiming.CurTime).TotalSeconds;
            IoCManager.Resolve<IChatManager>().DispatchServerMessage(shell.Player,
                $"Ещё рано. Возрождение доступно через {timeLeft:f0} секунд.");
            return;
        }

        _entitySystemManager.GetEntitySystem<GameTicker>().Respawn(shell.Player);
    }
}
