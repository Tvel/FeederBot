using CodeHollow.FeedReader;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace FeederBot.Jobs
{
    public class JobRunner : BackgroundService
    {
        private readonly int delay = int.Parse(Environment.GetEnvironmentVariable("Tick") ?? "1000");
        private readonly IMessageReceiver messageReceiver;
        public JobRunner(JobSchedulesStorage jobSchedulesStorage, ILogger<JobRunner> logger, IDateTimeProvider dateTimeProvider, IMessageReceiver messageReceiver)
        {
            JobSchedulesStorage = jobSchedulesStorage;
            Logger = logger;
            DateTimeProvider = dateTimeProvider;
            this.messageReceiver = messageReceiver;
        }

        public JobSchedulesStorage JobSchedulesStorage { get; }
        public ILogger<JobRunner> Logger { get; }
        public IDateTimeProvider DateTimeProvider { get; }
        
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Logger.LogInformation($"Feeder started");

            while (true)
            {
                try
                {
                    if (stoppingToken.IsCancellationRequested) throw new TaskCanceledException();

                    foreach (var job in JobSchedulesStorage.Jobs) { 
                        DateTime next = JobSchedulesStorage.GetNextOccurrence(job);
                        Logger.LogDebug($"Try Job[{job}]: {next:O}: Go? : {DateTimeProvider.Past(next)}");
                        if (DateTimeProvider.Past(next))
                        {
                            await ReadFeeds(job);
                        }
                    }
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception e) when (e is OperationCanceledException or TaskCanceledException)
                {
                    Logger.LogDebug($"Cancelled, closing");
                    return;
                }
            }
        }

        private async Task ReadFeeds(Job job)
        {
            var feed = await FeedReader.ReadAsync(job.Data);
            
            DateTime lastItem = JobSchedulesStorage.GetLastItem(job);

            DateTime lastPubDate = lastItem;
            foreach (var item in feed.Items)
            {
                if (item.PublishingDate <= lastItem) break;

                await messageReceiver.Send(item.Link);
                
                if (item.PublishingDate > lastPubDate)
                {
                    lastPubDate = item.PublishingDate ?? lastItem;
                }
            }
            
            await JobSchedulesStorage.SaveLastRun(job, DateTimeProvider.Now());
            await JobSchedulesStorage.SaveLastItem(job, lastPubDate);
        }
    }
}
