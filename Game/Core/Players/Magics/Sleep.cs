using System;

namespace Game.Core;

public class Sleep : Magic
{
    public Sleep() : base("スリープ", 300) { }

    protected override string ExecuteEffect(Player caster, Player target)
    {
        target.SleepTurns = 1;
        return $"{target.Name} は眠ってしまった！（次回1回休み）";
    }
}