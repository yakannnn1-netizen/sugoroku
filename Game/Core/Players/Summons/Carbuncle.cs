namespace Game.Core;

public class Carbuncle : Summon
{
    public Carbuncle() : base("カーバンクル", 120, 1.2) { }

    public override void OnSquareUpgraded(PropertySquare square, Player owner)
    {
        // レベルアップのお祝いに、オーナーに50クリスタル還元する能力
        System.Console.WriteLine($"{Name}の能力発動！{owner.Name}に50クリスタルが還元された！");
        owner.AddCrystal(50);
    }
}