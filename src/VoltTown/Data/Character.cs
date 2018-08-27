using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace VoltTown.Data.Game
{
    public class Character
    {
        public int CharacterId { get; set; }

        public ulong DiscordUserId { get; set; }

        [InverseProperty("Owner")]
        public List<Plot> OwnedPlots { get; set; }
    }
}
