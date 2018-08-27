using McMaster.Extensions.CommandLineUtils;

namespace VoltTown.Commands
{
    public partial class CommandService
    {
        private void AddCreateCommands()
        {
            _adminApp.Command("create", cmd =>
            {
                cmd.Command("area", areaCmd =>
                {
                    var name = areaCmd.Argument("name", "Name of the area to be created").IsRequired()
                        .Accepts(v => v.MinLength(4).MaxLength(24));
                    areaCmd.OnExecute(() =>
                    {
                        _game.CreateArea(name.Value);
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
                        _game.CreatePlot(areaName.Value, int.Parse(address.Value));
                        return 0;
                    });
                });
            });
        }
    }
}
