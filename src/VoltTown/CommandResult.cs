namespace VoltTown
{
    public struct CommandResult
    {
        public string Message { get; set; }
        public bool IsSuccess { get; set; }

        public CommandResult(string msg, bool isSuccess)
        {
            Message = msg;
            IsSuccess = isSuccess;
        }

        public static CommandResult Success(string msg) => new CommandResult(msg, true);
        public static CommandResult Failure(string msg) => new CommandResult(msg, false);
    }
}
