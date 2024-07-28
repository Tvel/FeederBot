using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace FeederBot.Jobs.Storage;

public class DatabaseConfig
{
    public string ConnectionString { get; set; } = null!;
}

public interface IDatabaseBootstrap
{
    void Setup();
}

public class DatabaseBootstrap : IDatabaseBootstrap
{
    private readonly IOptions<DatabaseConfig> databaseConfig;

    public DatabaseBootstrap(IOptions<DatabaseConfig> databaseConfig)
    {
        this.databaseConfig = databaseConfig;
    }

    public void Setup()
    {
        using var connection = new SqliteConnection(databaseConfig.Value.ConnectionString);

        void AddJobs()
        {
            var table = connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Jobs';");
            var tableName = table.FirstOrDefault();
            if (!string.IsNullOrEmpty(tableName) && tableName == "Jobs")
                return;

            connection.Execute("Create Table Jobs (" +
                               "Id VARCHAR(100) PRIMARY KEY NOT NULL," +
                               "UserId VARCHAR(100) NOT NULL," +
                               "Name VARCHAR(1000) NOT NULL," +
                               "Cron VARCHAR(256) NOT NULL," +
                               "Data VARCHAR(1000) NOT NULL," +
                               "UploadedTime DATETIME NOT NULL," +
                               "Description VARCHAR(1000) NULL," +
                               "Enabled INTEGER(0) NOT NULL)" +
                               ";");
        }
        AddJobs();

        void AddLastJobRuns()
        {
            var table = connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'LastJobRuns';");
            var tableName = table.FirstOrDefault();
            if (!string.IsNullOrEmpty(tableName) && tableName == "LastJobRuns")
                return;

            connection.Execute("Create Table LastJobRuns (" +
                               "JobId VARCHAR(100) NOT NULL," +
                               "RunTime DATETIME NOT NULL," +
                               "UNIQUE(JobId)" +
                               "FOREIGN KEY (JobId) REFERENCES Jobs (Id) ON DELETE CASCADE ON UPDATE NO ACTION" +
                               ");");

            connection.Execute("CREATE INDEX IF NOT EXISTS LastJobRuns_JobId ON LastJobRuns (JobId);");

        }
        AddLastJobRuns();

        void AddLastJobItems()
        {
            var table = connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'LastJobItems';");
            var tableName = table.FirstOrDefault();
            if (!string.IsNullOrEmpty(tableName) && tableName == "LastJobItems")
                return;

            connection.Execute("Create Table LastJobItems (" +
                               "JobId VARCHAR(100) NOT NULL," +
                               "LastTime DATETIME NOT NULL," +
                               "UNIQUE(JobId)" +
                               "FOREIGN KEY (JobId) REFERENCES Jobs (Id) ON DELETE CASCADE ON UPDATE NO ACTION" +
                               ");");

            connection.Execute("CREATE INDEX IF NOT EXISTS LastJobRuns_JobId ON LastJobItems (JobId);");

        }
        AddLastJobItems();


        void AddEnabledColumn()
        {
            var table = connection.Query<string>("SELECT name FROM sqlite_master WHERE type='table' AND name = 'Jobs' AND sql like '%Enabled%';");
            var tableName = table.FirstOrDefault();

            if (!string.IsNullOrEmpty(tableName) && tableName == "Jobs") return;
            
            connection.Execute("alter table Jobs add Enabled integer(0) default 1 not null;");
        }
        AddEnabledColumn();
    }
}
