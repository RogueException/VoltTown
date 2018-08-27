using Microsoft.EntityFrameworkCore;
using System;
using Voltaic.Logging;
using VoltTown.Data;
using VoltTown.Data.Game;

namespace VoltTown.Game
{
    public partial class GameService
    {
        public event Action<Area> AreaAdded, AreaUpdated;
        public event Action<Plot> PlotAdded, PlotUpdated;

        private readonly ILogger _logger;
        private readonly GameDbContext _db;

        public GameService(LogManager log, GameDbContext db)
        {
            _logger = log.CreateLogger("Game");

            _db = db;
        }

        public void CreateArea(string name)
        {
            TaskUtils.Execute(async () =>
            {
                var area = new Area { Name = name };
                _db.Areas.Add(area);
                await _db.SaveChangesAsync();
                AreaAdded?.Invoke(area);
            });
        }

        public void CreatePlot(string areaName, int address)
        {
            TaskUtils.Execute(async () =>
            {
                var area = await _db.Areas.SingleAsync(x => x.Name.Equals(areaName, StringComparison.InvariantCultureIgnoreCase));
                var plot = new Plot { Name = "plot", Address = address, Area = area };
                _db.Plots.Add(plot);
                await _db.SaveChangesAsync();
                PlotAdded?.Invoke(plot);
            });
        }
    }
}
