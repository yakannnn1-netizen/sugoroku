namespace Game.Core;

public class CheckpointSquare : Square
{
    public int CheckpointId { get; } // 1〜4の番号など

    public CheckpointSquare(string id, string name, int checkpointId) : base(id, name)
    {
        CheckpointId = checkpointId;
    }

    public override void OnLanded(Player player)
    {
        CheckIn(player);
    }

    public override void OnPassed(Player player)
    {
        CheckIn(player);
    }

    private void CheckIn(Player player)
    {
        player.VisitCheckpoint(CheckpointId);
    }
}