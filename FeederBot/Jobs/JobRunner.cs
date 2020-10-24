﻿using CodeHollow.FeedReader;
using FeederBot.Discord;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FeederBot.Jobs
{
    public class JobRunner
    {
        private int delay = int.Parse(Environment.GetEnvironmentVariable("Tick") ?? "1000");
        private SingleChannelDiscordSender messageSender;
        public JobRunner(JobSchedulesStorage jobSchedulesStorage, ILogger<JobRunner> logger, IDateTimeProvider dateTimeProvider, SingleChannelDiscordSender messageSender)
        {
            JobSchedulesStorage = jobSchedulesStorage;
            Logger = logger;
            DateTimeProvider = dateTimeProvider;
            this.messageSender = messageSender;
        }

        public JobSchedulesStorage JobSchedulesStorage { get; }
        public ILogger<JobRunner> Logger { get; }
        public IDateTimeProvider DateTimeProvider { get; }

        public async Task Run(CancellationToken cancellationToken = default)
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested) throw new TaskCanceledException();

                        foreach (var job in JobSchedulesStorage.Jobs) { 
                            DateTime next = JobSchedulesStorage.GetNextOccurance(job);
                            Logger.LogDebug($"Try Job[{job}]: {next:O}: Go? : {DateTimeProvider.Past(next)}");
                            if (DateTimeProvider.Past(next))
                            {
                                await ReadFeeds(job);
                                await JobSchedulesStorage.SaveLastRun(job, DateTimeProvider.Now());
                            }
                        }

                        await Task.Delay(delay, cancellationToken);
                    }
                    catch (Exception e) when (e is OperationCanceledException or TaskCanceledException)
                    {
                        Logger.LogDebug($"Cancelled, closing");
                        return;
                    }
                }
            });
        }

        private async Task ReadFeeds(Job job)
        {
            var feed = await FeedReader.ReadAsync(job.Data);
            var lastRun = JobSchedulesStorage.GetLastRun(job);

            foreach (var item in feed.Items)
            {
                if (item.PublishingDate <= lastRun) break;

                await messageSender.Send(item.Link);
            }
        }
    }
}
