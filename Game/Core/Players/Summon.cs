namespace Game.Core;

public class Summon
{
    public string Name { get; }
    public int Cost { get; }
    public double TollMultiplier { get; }

    public Summon(string name, int cost, double tollMultiplier)
    {
        Name = name;
        Cost = cost;
        TollMultiplier = tollMultiplier;
    }

    public virtual void OnEnemyLanded(Player enemy) { }
    public virtual void OnSquareUpgraded(PropertySquare square, Player owner) { }
    public virtual void OnMagicTargeted(Player owner, Magic magic, Player caster) { }

}