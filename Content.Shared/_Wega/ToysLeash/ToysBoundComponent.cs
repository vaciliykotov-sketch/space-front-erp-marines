namespace Content.Shared._Wega.ToysLeash;

[RegisterComponent]
public sealed partial class ToysBoundComponent : Component
{
    [DataField]
    public EntityUid? Owner;
}
