using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Verbs;

    public sealed partial class VerbCategory
{
        public static readonly VerbCategory Interaction = // Space Front — панель взаимодействий
            new("verb-categories-interaction", "/Textures/Interface/VerbIcons/group.svg.192dpi.png");
}
