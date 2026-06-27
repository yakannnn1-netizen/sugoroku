using System.Collections.Generic;

namespace Game.Core;

public static class MagicCatalog
{
    public static readonly Magic Sleep = new Sleep();
    public static readonly Magic Seal = new SealMagic();
    public static readonly Magic Fire = new Fire();

    public static readonly IReadOnlyList<Magic> AllMagics = new List<Magic>
    {
        Sleep,
        Seal,
        Fire
    };
}