using Content.Shared._SpaceFront.Evacuation;
namespace Content.Shared._SpaceFront.Evacuation;

/// <summary>
/// Эвакуационная шлюпка успешно улетела (без крушения).
/// </summary>
public readonly record struct EvacuationSuccessfullyLaunchedEvent(EntityUid Grid);
