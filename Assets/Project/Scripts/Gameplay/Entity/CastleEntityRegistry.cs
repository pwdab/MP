namespace MP.Gameplay.Entity
{
    public static class CastleEntityRegistry
    {
        public static CastleEntity ActiveCastle { get; private set; }

        public static void Register(CastleEntity castle)
        {
            if (castle != null)
            {
                ActiveCastle = castle;
            }
        }

        public static void Unregister(CastleEntity castle)
        {
            if (ActiveCastle == castle)
            {
                ActiveCastle = null;
            }
        }
    }
}
