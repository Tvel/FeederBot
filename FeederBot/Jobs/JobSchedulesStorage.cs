using NCrontab;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeederBot.Jobs.Storage;
using FeederBot.System;

namespace FeederBot.Jobs;

public class JobSchedulesStorage
{
    private readonly ConcurrentDictionary<Job, ScheduleData> jobSchedules = new();
    private readonly Dictionary<string, DateTime> lastJobRuns;
    private Dictionary<string, DateTime> lastJobItems = new();

    private readonly IJobStorage jobStorage;
    private readonly IDateTimeProvider dateTimeProvider;

    public JobSchedulesStorage(IDateTimeProvider dateTimeProvider, IJobStorage jobStorage)
    {
        this.dateTimeProvider = dateTimeProvider;
        this.jobStorage = jobStorage;
        this.lastJobRuns = jobStorage.LastJobRuns;

        Refresh();
    }

    public IEnumerable<Job> Jobs => jobSchedules.Keys;

    public void Refresh()
    {
        jobSchedules.Clear();
        foreach (var job in jobStorage.Jobs)
        {
            jobSchedules.GetOrAdd(job, new ScheduleData(
                CrontabSchedule.Parse(job.Cron),
                jobStorage.LastJobRuns.ContainsKey(job.Data) ? jobStorage.LastJobRuns[job.Data] : default)
            );
        }
        lastJobItems = jobStorage.LastJobItems;
    }

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

    public async Task SaveLastRun(Job job, DateTime dateTime)
    {
        lastJobRuns[job.Data] = dateTime;
        await jobStorage.UpdateLastRun(job.Data, dateTime);
    }

    public DateTime GetLastRun(Job job)
    {
        return lastJobRuns.ContainsKey(job.Data) ? lastJobRuns[job.Data] : dateTimeProvider.Now().Date;
    }

    public async Task SaveLastItem(Job job, DateTime dateTime)
    {
        lastJobItems[job.Data] = dateTime;
        await jobStorage.UpdateLastItem(job.Data, dateTime);
    }

    public DateTime GetLastItem(Job job)
    {
        lastJobItems.TryGetValue(job.Data, out var item);
        
        return item == default ? dateTimeProvider.Now().Date : item;
    }
}
