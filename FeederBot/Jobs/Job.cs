using NCrontab;
using System;

namespace FeederBot.Jobs
{
    public record Job(string Cron, string Data);
    public record ScheduleData(CrontabSchedule Cron, DateTime Next);
}
