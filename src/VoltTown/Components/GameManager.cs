using McMaster.Extensions.CommandLineUtils;
using System;
using System.Linq;
using Voltaic.Logging;
using VoltTown.Data;
using VoltTown.Data.Game;

namespace VoltTown.Components
{
    public class GameManager : Component
    {
        private readonly ILogger _logger;

        public GameManager(LogManager log, GameDbContext db, DiscordChannelSync channels, DiscordMemberSync members, CommandManager commands)
        {
            _logger = log.CreateLogger("Game");

            commands.AdminCommand("create", cmd =>
            {
                cmd.Command("area", areaCmd =>
                {
                    var name = areaCmd.Argument("name", "Name of the area to be created").IsRequired()
                        .Accepts(v => v.MinLength(4).MaxLength(24));
                    areaCmd.OnExecute(() =>
                    {
                        var area = new Area { Name = name.Value };
                        channels.CreateArea(area);
                        db.Areas.Add(area);
                        db.SaveChanges();
                        return 0;
                    });
                });
                cmd.Command("plot", plotCmd =>
                {
                    var areaName = plotCmd.Argument("area", "Name of the area to place this room in").IsRequired()
                        .Accepts(v => v.MinLength(4).MaxLength(24));
                    var address = plotCmd.Argument("address", "Numbered address for this plot").IsRequired()
                        .Accepts(v => v.MinLength(3).MaxLength(3));
                    plotCmd.OnExecute(() =>
                    {
                        var area = db.Areas.Single(x => x.Name.Equals(areaName.Value, StringComparison.OrdinalIgnoreCase));
                        var plot = new Plot { Name = "plot", Area = area, Address = ushort.Parse(address.Value) };
                        channels.CreatePlot(plot, area.DiscordCategoryId);
                        db.Plots.Add(plot);
                        db.SaveChanges();
                        return 0;
                    });
                });
            });
        }
    }
}
