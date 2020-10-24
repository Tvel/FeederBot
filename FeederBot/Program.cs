using FeederBot.Discord;
using FeederBot.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FeederBot
{
    class Program
    {
        static async Task Main()
        {
            IServiceCollection services = new ServiceCollection();
            Startup startup = new Startup();
            startup.ConfigureServices(services);

            using var cancellationTokenSource = new CancellationTokenSource();

            var keyBoardTask = Task.Run(() =>
            {
                //Console.WriteLine("Press enter to cancel");
                Console.ReadKey();

                // Cancel the task
                cancellationTokenSource.Cancel();
            });

            await Start(services.BuildServiceProvider(), cancellationTokenSource.Token);
        }

        static async Task Start(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            //var logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger<Program>();
            JobRunner jobRunner = serviceProvider.GetService<JobRunner>()!;
            SingleChannelDiscordSender discordclient = serviceProvider.GetService<SingleChannelDiscordSender>()!;

            var discordTask = discordclient.Start(cancellationToken);
            var jobTask = jobRunner.Run(cancellationToken);

            await Task.WhenAll(discordTask, jobTask);
        }
    }
}
