namespace VoltTown.Data.Game
{
    public class Plot
    {
        public int PlotId { get; set; }

        public string Name { get; set; }
        public int Address { get; set; }
        public ulong? DiscordChannelId { get; set; }

        public Area Area { get; set; }
        public int AreaId { get; set; }

        public Character Owner { get; set; }
        public int? OwnerId { get; set; }
    }
}
