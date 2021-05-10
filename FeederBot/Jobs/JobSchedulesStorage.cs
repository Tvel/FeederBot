using NCrontab;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeederBot.System;

namespace FeederBot.Jobs
{
    public class JobSchedulesStorage
    {
        private readonly ConcurrentDictionary<Job, ScheduleData> jobSchedules = new();
        private readonly Dictionary<string, DateTime> lastJobRuns;
        private readonly Dictionary<string, DateTime> lastJobItems;
        
        private readonly JobFileStorage jobFileStorage;
        private readonly IDateTimeProvider dateTimeProvider;

        public JobSchedulesStorage(IDateTimeProvider dateTimeProvider, JobFileStorage jobFileStorage)
        {
            this.dateTimeProvider = dateTimeProvider;
            this.jobFileStorage = jobFileStorage;
            this.lastJobRuns = jobFileStorage.LastJobRuns;
            this.lastJobItems = jobFileStorage.LastJobItems;

            foreach (var job in jobFileStorage.Jobs)
            {
                jobSchedules.GetOrAdd(job, new ScheduleData(
                        CrontabSchedule.Parse(job.Cron),
                        jobFileStorage.LastJobRuns.ContainsKey(job.Data) ? jobFileStorage.LastJobRuns[job.Data] : default)
                    );
            }
        }

        public IEnumerable<Job> Jobs => jobSchedules.Keys;

        public DateTime GetNextOccurrence(Job job)
        {
            var data = jobSchedules.GetOrAdd(job, new ScheduleData(CrontabSchedule.Parse(job.Cron), default));

            jobSchedules.AddOrUpdate(job,
                j =>
                {
                    var cron = CrontabSchedule.Parse(j.Cron);
                    return new ScheduleData(cron, cron.GetNextOccurrence(dateTimeProvider.Now()));
                },
                (j, ls) => new ScheduleData(ls.Cron, ls.Cron.GetNextOccurrence(dateTimeProvider.Now())));

            return data.Next;
        }

        public Task SaveLastRun(Job job, DateTime dateTime)
        {
            lastJobRuns[job.Data] = dateTime;
            jobFileStorage.UpdateLastRun(job.Data, dateTime);

            return Task.CompletedTask;
        }

        public DateTime GetLastRun(Job job)
        {
            return lastJobRuns.ContainsKey(job.Data) ? lastJobRuns[job.Data] : dateTimeProvider.Now().Date;
        }
        
        public Task SaveLastItem(Job job, DateTime dateTime)
        {
            lastJobItems[job.Data] = dateTime;
            jobFileStorage.UpdateLastItem(job.Data, dateTime);

            return Task.CompletedTask;
        }

        public DateTime GetLastItem(Job job)
        {
            return lastJobItems.ContainsKey(job.Data) ? lastJobItems[job.Data] : dateTimeProvider.Now().Date;
        }
    }
}
