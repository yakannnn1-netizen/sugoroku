using System.Collections.Generic;

namespace Game.Core;

public class Inventory
{
    public const int MaxSummons = 8;
    public const int MaxMagics = 4;

    public List<Summon> Summons { get; } = new List<Summon>();
    public List<Magic> Magics { get; } = new List<Magic>();

    public bool TryAddSummon(Summon summon)
    {
        if (Summons.Count >= MaxSummons) return false;
        
        Summons.Add(summon);
        return true;
    }

    public bool TryAddMagic(Magic magic)
    {
        if (Magics.Count >= MaxMagics) return false;
        
        Magics.Add(magic);
        return true;
    }

    public bool TryConsumeMagicByName(string magicName)
    {
        var targetMagic = Magics.Find(m => m.Name == magicName);
        if (targetMagic != null)
        {
            Magics.Remove(targetMagic);
            return true;
        }
        return false;
    }

    public void RemoveSummon(Summon summon) => Summons.Remove(summon);
    public void RemoveMagic(Magic magic) => Magics.Remove(magic);
}