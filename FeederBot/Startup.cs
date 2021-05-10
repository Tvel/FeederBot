using System;
using Discord.WebSocket;
using FeederBot.Discord;
using FeederBot.Jobs;
using FeederBot.System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FeederBot
{
    public class Startup
    {
        IConfigurationRoot Configuration { get; }

        public Startup()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.AddConfiguration(Configuration);
                builder.AddConsole();
            });
            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug);

            services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            services.AddSingleton<JobFileStorage>();
            services.AddSingleton<JobSchedulesStorage>();
            services.AddSingleton<JobRunner>();
            services.AddHostedService(static s => s.GetService<JobRunner>());

            services.AddSingleton<DiscordSocketClient>();
            services.AddSingleton<SingleChannelDiscordSender>();
            services.AddSingleton<IMessageReceiver>(static s => s.GetService<SingleChannelDiscordSender>() ?? throw new NullReferenceException("Invalid Receiver"));
            services.AddHostedService(static s => s.GetService<SingleChannelDiscordSender>());
        }
    }
}
