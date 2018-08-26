using System.Collections.Generic;

namespace VoltTown.Data.Game
{
    public class Area
    {
        public int AreaId { get; set; }
        
        public string Name { get; set; }
        public ulong? DiscordCategoryId { get; set; }

        public List<Plot> Rooms { get; set; }
    }
}
