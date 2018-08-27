using Microsoft.EntityFrameworkCore;

namespace VoltTown.Data
{
    public class GameDbContext : DbContext
    {
        public DbSet<Game.Area> Areas { get; set; }
        public DbSet<Game.Character> Characters { get; set; }
        public DbSet<Game.Plot> Plots { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(@"Data Source=game.db;");
        }

        public static GameDbContext Load()
        {
            var db = new GameDbContext();
            db.Database.EnsureCreated();
            return db;
        }
    }
}
