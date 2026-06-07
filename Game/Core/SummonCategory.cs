using System.Collections.Generic;

namespace Game.Core;

public static class SummonCatalog
{
    public static readonly Summon Goblin = new Goblin();
    public static readonly Summon Carbuncle = new Carbuncle();

    public static readonly IReadOnlyList<Summon> AllSummons = new List<Summon>
    {
        Goblin,
        Carbuncle
    };
}