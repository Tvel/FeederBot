using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FeederBot.Jobs
{
    public class JobFileStorage
    {
        private readonly string path = Environment.GetEnvironmentVariable("JobsDb") ?? "jobs.json";
        private readonly JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
        private SavedStorage SavedStorage { get; } = new SavedStorage();

        public JobFileStorage()
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                SavedStorage = JsonSerializer.Deserialize<SavedStorage>(json)!;
            }
        }

        public IEnumerable<Job> Jobs => SavedStorage.Jobs;
        public Dictionary<string, DateTime> LastJobRuns => SavedStorage.LastJobRuns;
        public Dictionary<string, DateTime> LastJobItems => SavedStorage.LastJobItems;
        
        public void UpdateLastRun(string Data, DateTime lastRun)
        {
            LastJobRuns[Data] = lastRun;
            File.WriteAllText(path, JsonSerializer.Serialize(SavedStorage, options));
        }
        
        public void UpdateLastItem(string Data, DateTime lastItem)
        {
            LastJobItems[Data] = lastItem;
            File.WriteAllText(path, JsonSerializer.Serialize(SavedStorage, options));
        }
    }

    public class SavedStorage
    {
        public IEnumerable<Job> Jobs { get; }
        public Dictionary<string, DateTime> LastJobRuns { get; }
        public Dictionary<string, DateTime> LastJobItems { get; }
        
        public SavedStorage()
        {
            Jobs = Array.Empty<Job>();
            LastJobRuns = new Dictionary<string, DateTime>();
            LastJobItems = new Dictionary<string, DateTime>();
        }
    }
}
