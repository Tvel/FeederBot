
using System.Threading.Tasks;
using FeederBot.Jobs;
using Microsoft.Extensions.Logging;

namespace FeederBot.Discord
{
    public class LogMessageReceiver : IMessageReceiver
    {
        private readonly ILogger<LogMessageReceiver> logger;

        public LogMessageReceiver(ILogger<LogMessageReceiver> logger)
        {
            this.logger = logger;
        }

        public ValueTask Send(string msg)
        {
            logger.LogDebug(msg);
            return ValueTask.CompletedTask;
        }
    }
}