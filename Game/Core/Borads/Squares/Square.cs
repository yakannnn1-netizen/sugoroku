using System.Collections.Generic;

namespace Game.Core;

public abstract class Square
{
    public string Id { get; }
    public string Name { get; set; }
    public List<Square> NextSquares { get; } = new List<Square>();

    protected Square(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public abstract void OnLanded(Player player);
    public virtual void OnPassed(Player player) { }
}