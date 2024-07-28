using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using FeederBot.Controllers.Helpers;
using FeederBot.Jobs;
using FeederBot.Jobs.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeederBot.Controllers;

[Authorize]
[ApiController]
[Route("/api/[controller]")]
public class Add : Controller
{
    private readonly IJobApiStorage jobApiStorage;
    private readonly JobSchedulesStorage jobSchedulesStorage;

    public Add(IJobApiStorage jobApiStorage, JobSchedulesStorage jobSchedulesStorage)
    {
        this.jobApiStorage = jobApiStorage;
        this.jobSchedulesStorage = jobSchedulesStorage;
    }

    [HttpPost]
    public async Task<IActionResult> Index([FromBody] JobAddViewModel job)
    {
        try
        {
            await jobApiStorage.Add(new JobAddModel()
            {
                Id = job.Id,
                UserId = job.UserId,
                Name = job.Name,
                Cron = job.Cron,
                Data = job.Data,
                Enabled = job.Enabled
            });
        }
        catch (Exception e)
        {
            ModelState.AddModelError("Job", e.Message);
            return ValidationProblem();
        }

        jobSchedulesStorage.Refresh();

        return Ok();
    }

    public class JobAddViewModel
    {
        [StringLength(100, ErrorMessage = "{0} length must be between {2} and {1}.", MinimumLength = 1)]
        public string Id { get; set; } = null!;

        [StringLength(100, ErrorMessage = "{0} length must be between {2} and {1}.", MinimumLength = 1)]
        public string UserId { get; set; } = null!;

        [StringLength(100, ErrorMessage = "{0} length must be between {2} and {1}.", MinimumLength = 1)]
        public string Name { get; set; } = null!;

        [Cron]
        public string Cron { get; set; } = null!;

        [Url]
        public string Data { get; set; } = null!;
        
        public bool Enabled { get; set; }
    }
}
