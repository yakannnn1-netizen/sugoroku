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

    public abstract System.Threading.Tasks.Task OnLandedAsync(Player player);
    public virtual System.Threading.Tasks.Task OnPassedAsync(Player player)
    {
        return System.Threading.Tasks.Task.CompletedTask;
    }
}