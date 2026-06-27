using System.Collections.Generic;

namespace MP.Gameplay.Entity
{
    public static class PlayerEntityRegistry
    {
        private static readonly List<PlayerEntity> Players = new();

        public static IReadOnlyList<PlayerEntity> ActivePlayers => Players;

        public static void Register(PlayerEntity player)
        {
            if (player != null && !Players.Contains(player))
            {
                Players.Add(player);
            }
        }

        public static void Unregister(PlayerEntity player)
        {
            Players.Remove(player);
        }
    }
}
