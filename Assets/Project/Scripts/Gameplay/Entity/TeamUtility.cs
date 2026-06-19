namespace MP.Gameplay.Entity
{
    public static class TeamUtility
    {
        public static bool AreEnemies(TeamId a, TeamId b)
        {
            return a != TeamId.Neutral && b != TeamId.Neutral && a != b;
        }
    }
}
