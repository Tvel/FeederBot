using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace FeederBot.Jobs.Storage;

public interface IJobStorage
{
    ICollection<Job> Jobs { get; }

    Dictionary<string, DateTime> LastJobRuns { get; }

    Dictionary<string, DateTime> LastJobItems { get; }

    Task UpdateLastRun(string Data, DateTime lastRun);

    Task UpdateLastItem(string Data, DateTime lastItem);
}

public interface IJobApiStorage
{
    [return: NotNull]
    Task<IEnumerable<JobListModel>> GetAll();

    Task Add(JobAddModel jobAddModel);

    Task Edit(string id, JobEditModel jobAddModel);

    Task Delete(string id);
}

public class JobAddModel
{
    public string Id { get; init; } = null!;

    public string UserId { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string Cron { get; init; } = null!;

    public string Data { get; init; } = null!;
}

public class JobEditModel
{
    public string UserId { get; init; } = null!;

    public string Name { get; init; } = null!;

    public string Cron { get; init; } = null!;

    public string Data { get; init; } = null!;
}

public class JobListModel
{
    public string Id { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Cron { get; set; } = null!;

    public string Data { get; set; } = null!;

    public DateTime LastTime { get; set; }

    public DateTime RunTime { get; set; }
}
