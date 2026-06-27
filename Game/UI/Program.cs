using System;
using Game.UI;

internal class Program
{
    [STAThread]
    static void Main()
    {
        using var game = new SugorokuGame();
        game.Run();
    }
}