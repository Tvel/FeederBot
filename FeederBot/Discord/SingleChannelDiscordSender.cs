using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using FeederBot.Jobs;
using Microsoft.Extensions.Options;

namespace FeederBot.Discord
{
    public class SingleChannelDiscordSender : BackgroundService, IMessageReceiver
    {
        private readonly Channel<string> channel;
        private readonly DiscordSocketClient discordSocketClient;
        private readonly IOptions<DiscordSettings> discordSettings;
        private readonly ILogger<SingleChannelDiscordSender> logger;
        private bool logged;
        private readonly ulong channelId;
        private readonly ulong guildId;

        public SingleChannelDiscordSender(DiscordSocketClient discordSocketClient, IOptions<DiscordSettings> discordSettings, ILogger<SingleChannelDiscordSender> logger)
        {
            this.channel = Channel.CreateBounded<string>(100);
            this.discordSocketClient = discordSocketClient;
            this.discordSettings = discordSettings;
            this.logger = logger;

            channelId = ulong.Parse(discordSettings.Value.ChannelId);
            guildId = ulong.Parse(discordSettings.Value.GuildId);
        }

        public ValueTask Send(string msg)
        {
            return channel.Writer.WriteAsync(msg);
        }
        
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await discordSocketClient.LoginAsync(TokenType.Bot, discordSettings.Value.DiscordToken);
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
                    logger.LogInformation($"Cancelled, closing");
                    return;
                }
            }
        }
    }
}
