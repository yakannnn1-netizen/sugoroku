namespace Game.Core;

public class CheckpointSquare : Square
{
    public int CheckpointId { get; } // 1〜4の番号など

    public CheckpointSquare(string id, string name, int checkpointId) : base(id, name)
    {
        CheckpointId = checkpointId;
    }

    public override System.Threading.Tasks.Task OnLandedAsync(Player player)
    {
        CheckIn(player);
        return System.Threading.Tasks.Task.CompletedTask;
    }

    public override System.Threading.Tasks.Task OnPassedAsync(Player player)
    {
        CheckIn(player);
        return System.Threading.Tasks.Task.CompletedTask;
    }

    private void CheckIn(Player player)
    {
        player.VisitCheckpoint(CheckpointId);
    }
}