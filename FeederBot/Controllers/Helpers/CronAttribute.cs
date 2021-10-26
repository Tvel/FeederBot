using System.ComponentModel.DataAnnotations;
using NCrontab;

namespace FeederBot.Controllers.Helpers;

public class CronAttribute : ValidationAttribute
{
    public string GetErrorMessage(string value) =>
        $"Invalid Cron \"{value}\"";

    protected override ValidationResult IsValid(object? value,
                                                ValidationContext validationContext)
    {
        var cronString = (string)(value ?? string.Empty);

        var result = CrontabSchedule.TryParse(cronString);

        if (result is null)
        {
            return new ValidationResult(GetErrorMessage(cronString));
        }

        return ValidationResult.Success!;
    }
}
