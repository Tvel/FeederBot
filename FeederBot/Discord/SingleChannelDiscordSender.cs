using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using FeederBot.Jobs;

using Microsoft.Extensions.Hosting;

namespace FeederBot.Discord
{
    public class SingleChannelDiscordSender : BackgroundService, IMessageReceiver
    {
        private Channel<string> channel;
        private DiscordSocketClient discordSocketClient;
        ILogger<SingleChannelDiscordSender> logger;
        bool logged;
        ulong channelId = ulong.Parse(Environment.GetEnvironmentVariable("ChannelId")!);
        ulong guildId = ulong.Parse(Environment.GetEnvironmentVariable("GuildId")!);

        public SingleChannelDiscordSender(DiscordSocketClient discordSocketClient, ILogger<SingleChannelDiscordSender> logger)
        {
            this.channel = Channel.CreateBounded<string>(100);
            this.discordSocketClient = discordSocketClient;
            this.logger = logger;
        }

        public ValueTask Send(string msg)
        {
            return channel.Writer.WriteAsync(msg);
        }
        
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await discordSocketClient.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DiscordToken"));
            await discordSocketClient.StartAsync();
            discordSocketClient.Ready += DiscordConnected;

            Task DiscordConnected()
            {
                logged = true;
                logger.LogInformation($"Discord Logged in");

                return Task.CompletedTask;
            }
            
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (true)
            {
                try
                {
                    while (logged == false) await Task.Delay(1000, stoppingToken);

                    var msg = await channel.Reader.ReadAsync(stoppingToken);

                    if (stoppingToken.IsCancellationRequested) throw new TaskCanceledException();

                    var dguild = discordSocketClient.GetGuild(guildId);
                    var dchannel = dguild.GetTextChannel(channelId);
                    await dchannel.SendMessageAsync(msg);

                    logger.LogDebug($"msg recieved: {msg}");
                }
                catch (Exception e) when (e is OperationCanceledException or TaskCanceledException)
                {
                    logger.LogDebug($"Cancelled, closing");
                    return;
                }
            }
        }
    }
}
