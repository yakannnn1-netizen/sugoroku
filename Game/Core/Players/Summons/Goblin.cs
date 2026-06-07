namespace Game.Core;

public class Goblin : Summon
{
    public Goblin() : base("ゴブリン", 100, 1.2)
    {
    }

    public override void OnEnemyLanded(Player enemy)
    {
    }
}