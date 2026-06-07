namespace Game.Core;

public class MagicCastResult
{
    public bool WasCountered { get; }
    public string Message { get; }

    public MagicCastResult(bool wasCountered, string message)
    {
        WasCountered = wasCountered;
        Message = message;
    }
}