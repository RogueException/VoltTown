using McMaster.Extensions.CommandLineUtils;
using System;
using Voltaic.Logging;

namespace VoltTown.Components
{
    public class CommandManager
    {
        private readonly ILogger _logger;
        private readonly CommandLineApplication _adminApp, _userApp;

        public CommandManager(LogManager log)
        {
            _logger = log.CreateLogger("Commands");

            _adminApp = new CommandLineApplication(false);
            _userApp = new CommandLineApplication(false);
        }

        public bool AdminExecute(ulong userId, string input)
        {
            var args = input.Split(' ');

            ClearState(_adminApp);
            if (_adminApp.Execute(args) == 0)
                return true;

            ClearState(_userApp);
            return _userApp.Execute(args) == 0;
        }
        public bool Execute(ulong userId, string input)
        {
            var args = input.Split(' ');

            ClearState(_userApp);
            return _userApp.Execute(args) == 0;
        }

        private void ClearState(CommandLineApplication app)
        {
            // CommandLineApplication wasn't designed to be called multiple times, so we need to clear state ourselves
            foreach (var argument in app.Arguments)
                argument.Values.Clear();
            foreach (var cmd in app.Commands)
                ClearState(cmd);
        }

        public void AdminCommand(string name, Action<CommandLineApplication> configuration)
        {
            _adminApp.Command(name, configuration);
        }
        public void Command(string name, Action<CommandLineApplication> configuration)
        {
            _userApp.Command(name, configuration);
        }
    }
}
