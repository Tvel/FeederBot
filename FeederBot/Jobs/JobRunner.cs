using CodeHollow.FeedReader;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using FeederBot.System;
using Microsoft.Extensions.Hosting;

namespace FeederBot.Jobs
{
    public class JobRunner : BackgroundService
    {
        private readonly int delay = int.Parse(Environment.GetEnvironmentVariable("Tick") ?? "1000");
        private readonly IMessageReceiver messageReceiver;
        private readonly JobSchedulesStorage jobSchedulesStorage;
        private readonly ILogger<JobRunner> logger;
        private readonly IDateTimeProvider dateTimeProvider;

        public JobRunner(JobSchedulesStorage jobSchedulesStorage, ILogger<JobRunner> logger, IDateTimeProvider dateTimeProvider, IMessageReceiver messageReceiver)
        {
            this.jobSchedulesStorage = jobSchedulesStorage;
            this.logger = logger;
            this.dateTimeProvider = dateTimeProvider;
            this.messageReceiver = messageReceiver;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation($"Feeder started");

            while (true)
            {
                try
                {
                    if (stoppingToken.IsCancellationRequested) throw new TaskCanceledException();

                    foreach (var job in jobSchedulesStorage.Jobs) { 
                        DateTime next = jobSchedulesStorage.GetNextOccurrence(job);
                        logger.LogDebug($"Try Job[{job}]: {next:O}: Go? : {dateTimeProvider.Past(next)}");
                        if (dateTimeProvider.Past(next))
                        {
                            await ReadFeeds(job);
                        }
                    }
                    await Task.Delay(delay, stoppingToken);
                }
                catch (Exception e) when (e is OperationCanceledException or TaskCanceledException)
                {
                    logger.LogInformation($"Cancelled, closing");
                    return;
                }
            }
        }

        private async Task ReadFeeds(Job job)
        {
            var feed = await FeedReader.ReadAsync(job.Data);
            
            DateTime lastItem = jobSchedulesStorage.GetLastItem(job);

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
            
            await jobSchedulesStorage.SaveLastRun(job, dateTimeProvider.Now());
            await jobSchedulesStorage.SaveLastItem(job, lastPubDate);
        }
    }
}
