using CodeHollow.FeedReader;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using FeederBot.System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;

namespace FeederBot.Jobs
{
    public class JobRunner : BackgroundService
    {
        private readonly int delay;
        private readonly IMessageReceiver messageReceiver;
        private readonly JobSchedulesStorage jobSchedulesStorage;
        private readonly ILogger<JobRunner> logger;
        private readonly IDateTimeProvider dateTimeProvider;

        public JobRunner(JobSchedulesStorage jobSchedulesStorage, ILogger<JobRunner> logger, IDateTimeProvider dateTimeProvider, IMessageReceiver messageReceiver, IOptions<FeederSettings> feederSettings)
        {
            this.jobSchedulesStorage = jobSchedulesStorage;
            this.logger = logger;
            this.dateTimeProvider = dateTimeProvider;
            this.messageReceiver = messageReceiver;
            delay = int.Parse(feederSettings.Value.Tick);
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
            Feed? feed;
            try
            {
                feed = await Policy.TimeoutAsync(30)
                           .ExecuteAsync(async () => await FeedReader.ReadAsync(job.Data));
            }
            catch (TimeoutRejectedException e)
            {
                logger.LogInformation($"Timeout for {job.Data}, skipping");
                return;
            }
            
            DateTime lastItem = jobSchedulesStorage.GetLastItem(job);

            DateTime lastPubDate = lastItem;
            foreach (var item in feed.Items)
            {
                if (item.PublishingDate is null)
                {
                    if (item.PublishingDateString.EndsWith(" UTC"))
                    {
                        DateTime.TryParse(item.PublishingDateString.Replace(" UTC", " GMT"), out var pubdate);
                        item.PublishingDate = pubdate;
                    }
                }
                
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
