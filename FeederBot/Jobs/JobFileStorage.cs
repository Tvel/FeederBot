using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FeederBot.Jobs
{
    public class JobFileStorage
    {
        private string path = Environment.GetEnvironmentVariable("JobsDb") ?? "jobs.json";
        private JsonSerializerOptions options = new JsonSerializerOptions() { WriteIndented = true };
        private SavedStroage SavedStroage { get; } = new SavedStroage();

        public JobFileStorage()
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                SavedStroage = JsonSerializer.Deserialize<SavedStroage>(json)!;
            }
        }

        public IEnumerable<Job> Jobs => SavedStroage.Jobs;
        public Dictionary<string, DateTime> LastJobRuns => SavedStroage.LastJobRuns;

        public void UpdateLastRun(string Data, DateTime lastRun)
        {
            LastJobRuns[Data] = lastRun;
            File.WriteAllText(path, JsonSerializer.Serialize(SavedStroage, options));
        }
    }

    public class SavedStroage
    {
        public IEnumerable<Job> Jobs { get; set; }
        public Dictionary<string, DateTime> LastJobRuns { get; set; }

        public SavedStroage()
        {
            Jobs = Array.Empty<Job>();
            LastJobRuns = new Dictionary<string, DateTime>();
        }
    }
}
