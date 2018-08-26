namespace VoltTown.Data.Discord
{
    public class User
    {
        public ulong UserId { get; set; }

        public int? CharacterId { get; set; }
        public Game.Character Character { get; set; }
    }
}
