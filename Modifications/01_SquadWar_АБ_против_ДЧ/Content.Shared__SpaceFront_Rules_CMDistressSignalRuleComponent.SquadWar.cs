using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Rules;

public sealed partial class CMDistressSignalRuleComponent
{
// --- SquadWar («стенка на стенку») ---

    [DataField]
    public bool SquadWar;

    [DataField]
    public ResPath SquadWarSecondShipMap = new("/Maps/_SpaceFront/_RMC14/almayer.yml");

    [DataField]
    public EntProtoId SquadWarFogWall = "RMCFogWall30";

    [DataField]
    public TimeSpan SquadWarFogDuration = TimeSpan.FromMinutes(30);

    [DataField]
    public List<EntProtoId> SquadWarShipOneSquads = new() { "SquadCharlie", "SquadDelta" };

    [DataField]
    public List<EntProtoId> SquadWarShipTwoSquads = new() { "SquadAlpha", "SquadBravo" };

    [DataField]
    public string SquadWarLZ1 = "LZ1";

    [DataField]
    public string SquadWarLZ2 = "LZ2";

    [DataField]
    public EntityUid? ShipOneGrid;

    [DataField]
    public EntityUid? ShipTwoGrid;

    [DataField]
    public bool SquadWarShipOneUnlocked;

    [DataField]
    public bool SquadWarShipTwoUnlocked;

    [DataField]
    public bool SquadWarEnded;

    [DataField]
    public bool SquadWarFogSpawned;

    /// <summary>
    /// Линия тумана реально построена (после постройки повторных попыток не делаем).
    /// </summary>
    public bool SquadWarFogDone;

    /// <summary>
    /// MapId карты планеты — по нему переищем грид планеты, если тот пересоздаётся
    /// (SetUpStation удаляет грид-чертёж и создаёт новый).
    /// </summary>
    public MapId? SquadWarPlanetMapId;

    /// <summary>
    /// SquadWar: сколько «пятого отряда» (не-отрядных ролей) заспавнено на каждом корабле.
    /// </summary>
    [DataField]
    public int ShipOneNonSquad;

    [DataField]
    public int ShipTwoNonSquad;

    /// <summary>
    /// SquadWar: были ли игроки в командах (для проверки полного уничтожения).
    /// </summary>
    [DataField]
    public bool SquadWarHadABPlayer;

    [DataField]
    public bool SquadWarHadDCHPlayer;

    /// <summary>
    /// SquadWar: туман рассеялся (разрешает контроль планеты).
    /// </summary>
    [DataField]
    public bool SquadWarFogCleared;

    // Space Front: команды высадились на планету (для мгновенного рассеивания тумана).
    public bool SquadWarPlanetArrivedAB;
    public bool SquadWarPlanetArrivedDCH;

    // Space Front: цель подрыва (вражеский Алмаер) и запрет отключения авто-взведённых реакторов.
    public EntityUid? SquadWarScuttleTargetGrid;
    public bool SquadWarScuttleLocked;

    /// <summary>
    /// SquadWar: когда начался непрерывный контроль планеты одной командой.
    /// </summary>
    [DataField]
    public TimeSpan? SquadWarPlanetControlStart;

    /// <summary>
    /// SquadWar: команда, которая сейчас контролирует планету (0 — никто, 1 — АБ, 2 — ДЧ).
    /// </summary>
    [DataField]
    public int SquadWarControllingTeam;

    /// <summary>
    /// SquadWar: все игроки раскрыты на тактической карте (через час).
    /// </summary>
    [DataField]
    public bool SquadWarRevealed;

    /// <summary>
    /// SquadWar: сколько нужно непрерывно контролировать планету.
    /// </summary>
    [DataField]
    public TimeSpan SquadWarPlanetControlTime = TimeSpan.FromMinutes(1);

    /// <summary>
    /// SquadWar: какая команда была полностью уничтожена (0 — никто, 1 — ДЧ, 2 — АБ).
    /// В паре с SquadWarArrivedTeam даёт победителя: уничтожена ДЧ (1) + прибыла АБ (1) = победа АБ.
    /// </summary>
    [DataField]
    public int SquadWarEliminatedTeam;

    /// <summary>
    /// SquadWar: момент, когда в обеих командах не осталось живых активных игроков.
    /// Если это продолжается 5 минут — объявляется ничья. Сбрасывается при появлении выживших.
    /// </summary>
    [DataField]
    public TimeSpan? SquadWarDrawStart;

    /// <summary>
    /// SquadWar: начало удержания условий победы (условия должны продержаться 1 минуту).
    /// </summary>
    public TimeSpan? SquadWarVictoryHoldStart;

    /// <summary>
    /// SquadWar: команда-кандидат на уничтожение и начало удержания (1 минута без живых).
    /// </summary>
    public int SquadWarEliminationHoldTeam;
    public TimeSpan? SquadWarEliminationHoldStart;

    /// <summary>
    /// SquadWar: команда, чей шаттл высадился на вражеский Алмаер (0 — никто, 1 — АБ, 2 — ДЧ).
    /// </summary>
    [DataField]
    public int SquadWarArrivedTeam;

    /// <summary>
    /// SquadWar: красный код при штурме корабля ДЧ уже объявлен.
    /// </summary>
    [DataField]
    public bool SquadWarShipOneInvasionAlerted;

    /// <summary>
    /// SquadWar: красный код при штурме корабля АБ уже объявлен.
    /// </summary>
    [DataField]
    public bool SquadWarShipTwoInvasionAlerted;

    /// <summary>
    /// SquadWar: эвакуационная шлюпка успешно улетела.
    /// </summary>
    [DataField]
    public bool SquadWarEvacSucceeded;

    /// <summary>
    /// SquadWar: чей корабль эвакуировался (0 — никто, 1 — АБ, 2 — ДЧ).
    /// </summary>
    [DataField]
    public int SquadWarEvacShip;

    /// <summary>
    /// SquadWar: сколько ограниченных ролей (КО, КМ и т.д.) на каждом корабле.
    /// </summary>
    [DataField]
    public Dictionary<string, int> ShipOneLimitedRoles = new();

    [DataField]
    public Dictionary<string, int> ShipTwoLimitedRoles = new();

    [DataField]
    public TimeSpan SquadWarCheckEvery = TimeSpan.FromSeconds(10);
}
