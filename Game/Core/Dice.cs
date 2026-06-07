using System;

namespace Game.Core
{
    public class Dice
    {
        private readonly Random _random = new Random();
        public int Roll()
        {
            return _random.Next(1, 7);
        }
    }
}