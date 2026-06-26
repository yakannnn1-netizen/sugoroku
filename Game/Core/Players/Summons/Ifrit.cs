using System;

namespace Game.Core;

public class Ifrit : Summon
{
    public Ifrit() : base("イフリート", 300, 1.5) { }

    public override void OnEnemyLanded(Player enemy)
    {
        int burnDamage = 150;
        int actualDamage = enemy.Crystal >= burnDamage ? burnDamage : enemy.Crystal;
        enemy.AddCrystal(-actualDamage);

        System.Console.WriteLine($"{Name}の能力発動！{enemy.Name}に追加で{actualDamage}のクリスタルダメージを与えた！");
    }
}