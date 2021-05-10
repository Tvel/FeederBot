using System;

namespace FeederBot.System
{
    public interface IDateTimeProvider
    {
        public DateTime Now();
        public bool Past(DateTime d) => d < Now();
    }

    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime Now() => DateTime.Now;
    }
}
