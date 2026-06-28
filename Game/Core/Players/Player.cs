namespace Game.Core
{
    public class Player
    {
        public string Name{get;}
        public int Crystal { get; set; }
        public bool[] VisitedCheckpoints { get; } = new bool[4];
        public int SleepTurns { get; set; } = 0;
        public int SealTurns { get; set; } = 0;
        public bool IsStatusAffected => SleepTurns > 0 || SealTurns > 0;
        public Inventory Inventory { get; } = new Inventory();
        public Square CurrentSquare { get; set; }
        public int LapCount { get; set; } = 0; // 何週目かを管理

        public Player(string name, int crystal)
        {
            Name = name;
            Crystal = crystal;
        }

        public void AddCrystal(int amount)
        {
            Crystal += amount;
        }

        public int GetVisitedCheckpointCount()
        {
            return VisitedCheckpoints.Count(v => v);
        }

        public void VisitCheckpoint(int checkpointId)
        {
            int index = checkpointId - 1;
            if (index >= 0 && index < VisitedCheckpoints.Length)
            {
                VisitedCheckpoints[index] = true;
            }
        }

        public void ResetCheckpoints()
        {
            for (int i = 0; i < VisitedCheckpoints.Length; i++)
            {
                VisitedCheckpoints[i] = false;
            }
        }

        public void DecrementStatusEffects()
        {
            if (SleepTurns > 0) SleepTurns--;
            if (SealTurns > 0) SealTurns--;
        }
    }
}