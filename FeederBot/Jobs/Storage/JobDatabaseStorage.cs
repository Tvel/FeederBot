using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace FeederBot.Jobs.Storage;

public class JobDatabaseStorage : IJobStorage, IJobApiStorage
{
    private readonly IOptions<DatabaseConfig> databaseConfig;

    public JobDatabaseStorage(IOptions<DatabaseConfig> databaseConfig)
    {
        this.databaseConfig = databaseConfig;

        //var jobs = GetJobs();

        //Jobs = jobs.Select(x => new Job(x.Cron, x.Data)).ToList();
        //LastJobRuns = jobs.ToDictionary(x => x.Data, v => v.RunTime);
        //LastJobItems = jobs.ToDictionary(x => x.Data, v => v.LastTime);
    }

    public ICollection<Job> Jobs => GetJobs().Select(x => new Job(x.Cron, x.Data)).ToList();

    public Dictionary<string, DateTime> LastJobRuns => GetJobs().ToDictionary(x => x.Data, v => v.RunTime);

    public Dictionary<string, DateTime> LastJobItems => GetJobs().ToDictionary(x => x.Data, v => v.LastTime);

    public async Task UpdateLastRun(string data, DateTime lastRun)
    {
        await using var connection = new SqliteConnection(databaseConfig.Value.ConnectionString);

        var id = await connection.QueryFirstAsync<string>(@"SELECT Id 
                                              FROM Jobs 
                                              WHERE Jobs.Data = @Data", new { Data = data });

        await connection.ExecuteAsync(@"INSERT INTO LastJobRuns (JobId, RunTime) VALUES (@Id, @LastRun) ON CONFLICT(JobId) DO UPDATE SET RunTime = @LastRun", new
        {
            Id = id,
            LastRun = lastRun
        });
    }

    public async Task UpdateLastItem(string data, DateTime lastItem)
    {
        using var connection = new SqliteConnection(databaseConfig.Value.ConnectionString);

        var id = await connection.QueryFirstAsync<string>(@"SELECT Id 
                                              FROM Jobs 
                                              WHERE Jobs.Data = @Data", new { Data = data });

        await connection.ExecuteAsync(@"INSERT INTO LastJobItems (JobId, LastTime) VALUES (@Id, @LastItem) ON CONFLICT(JobId) DO UPDATE SET LastTime = @LastItem", new
        {
            Id = id,
            LastItem = lastItem
        });
    }

    private IEnumerable<JobQueryResult> GetJobs()
    {
        using var connection = new SqliteConnection(databaseConfig.Value.ConnectionString);

        IEnumerable<JobQueryResult>? queryModels = connection.Query<JobQueryResult>(@"
                    SELECT Cron, Data, li.LastTime, lr.RunTime FROM Jobs
                        LEFT JOIN LastJobItems li on li.JobId = Jobs.Id
                        LEFT JOIN LastJobRuns lr on lr.JobId = Jobs.Id
                     ;");

        return queryModels.ToList();
    }

    public async Task<IEnumerable<JobListModel>> GetAll()
    {
        using var connection = new SqliteConnection(databaseConfig.Value.ConnectionString);

        IEnumerable<JobListModel> queryModels = await connection.QueryAsync<JobListModel>(@"
                    SELECT Id, UserId, Name, Cron, Data, li.LastTime, lr.RunTime FROM Jobs
                        LEFT JOIN LastJobItems li on li.JobId = Jobs.Id
                        LEFT JOIN LastJobRuns lr on lr.JobId = Jobs.Id
                     ;");

        return queryModels.ToList();
    }

    public async Task Add(JobAddModel jobAddModel)
    {
        await using var connection = new SqliteConnection(databaseConfig.Value.ConnectionString);

        await connection.ExecuteAsync(@"INSERT INTO Jobs (Id, UserId, Name, Cron, Data, UploadedTime) VALUES (@Id, @UserId, @Name, @Cron, @Data, @UploadedTime)", new
        {
            Id = jobAddModel.Id,
            UserId = jobAddModel.UserId,
            Name = jobAddModel.Name,
            Cron = jobAddModel.Cron,
            Data = jobAddModel.Data,
            UploadedTime = DateTime.Now
        });
    }

    public async Task Edit(string id, JobEditModel jobAddModel)
    {
        await using var connection = new SqliteConnection(databaseConfig.Value.ConnectionString);

        await connection.ExecuteAsync(@"UPDATE Jobs 
                                                    SET UserId = @UserId, Name = @Name, Cron = @Cron, Data = @Data, UploadedTime = @UploadedTime 
                                                WHERE Id = @Id", new
        {
            Id = id,
            UserId = jobAddModel.UserId,
            Name = jobAddModel.Name,
            Cron = jobAddModel.Cron,
            Data = jobAddModel.Data,
            UploadedTime = DateTime.Now
        });
    }

    public async Task Delete(string id)
    {
        await using var connection = new SqliteConnection(databaseConfig.Value.ConnectionString);

        await connection.ExecuteAsync(@"DELETE FROM Jobs WHERE Id = @Id", new
        {
            Id = id,
        });
    }
}

class JobQueryResult
{
    public string Cron { get; set; } = null!;

    public string Data { get; set; } = null!;

    public DateTime LastTime { get; set; }

    public DateTime RunTime { get; set; }
}
