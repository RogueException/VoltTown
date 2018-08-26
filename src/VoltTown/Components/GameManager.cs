using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Voltaic.Logging;
using VoltTown.Data;
using VoltTown.Data.Game;

namespace VoltTown.Components
{
    public class GameManager
    {
        private readonly ILogger _logger;
        private readonly GameDbContext _db;
        private readonly DiscordManager _discord;


        public GameManager(LogManager log, GameDbContext db, DiscordManager discord, CommandManager commands)
        {
            _logger = log.CreateLogger("Game");

            _db = db;
            _discord = discord;

            commands.AdminCommand("create", cmd =>
            {
                cmd.Command("area", areaCmd =>
                {
                    var name = areaCmd.Argument("name", "Name of the area to be created").IsRequired()
                        .Accepts(v => v.MinLength(4).MaxLength(24));
                    areaCmd.OnExecute(() =>
                    {
                        var area = new Area { Name = name.Value };
                        _discord.CreateAreaChannel(area);
                        _db.Areas.Add(area);
                        _db.SaveChanges();
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
                        var area = _db.Areas.Single(x => x.Name.Equals(areaName.Value, StringComparison.OrdinalIgnoreCase));
                        var plot = new Plot { Name = "plot", Area = area, Address = ushort.Parse(address.Value) };
                        _discord.CreatePlotChannel(plot, area.DiscordCategoryId);
                        _db.Plots.Add(plot);
                        _db.SaveChanges();
                        return 0;
                    });
                });
            });
        }
    }
}
