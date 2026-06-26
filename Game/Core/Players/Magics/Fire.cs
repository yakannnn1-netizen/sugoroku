using System;

namespace Game.Core;

public class Fire : Magic
{
    public Fire() : base("ファイア", 500) { }

    protected override string ExecuteEffect(Player caster, Player target)
    {
        int damage = 300;

        // 払えるだけ払う（借金は一旦なし）
        int actualDamage = target.Crystal >= damage ? damage : target.Crystal;
        target.AddCrystal(-actualDamage);

        return $"{target.Name} に {actualDamage} のクリスタルダメージを与えた！";
    }
}