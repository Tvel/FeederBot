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
public class Edit : Controller
{
    private readonly IJobApiStorage jobApiStorage;
    private readonly JobSchedulesStorage jobSchedulesStorage;

    public Edit(IJobApiStorage jobApiStorage, JobSchedulesStorage jobSchedulesStorage)
    {
        this.jobApiStorage = jobApiStorage;
        this.jobSchedulesStorage = jobSchedulesStorage;
    }

    [HttpPut]
    [Route("{id}")]
    public async Task<IActionResult> Put([FromRoute] string id, [FromBody] JobEditViewModel job)
    {
        try
        {
            await jobApiStorage.Edit(id,
                new JobEditModel() { UserId = job.UserId, Name = job.Name, Cron = job.Cron, Data = job.Data, Enabled = job.Enabled});
        }
        catch (Exception e)
        {
            ModelState.AddModelError("Job", e.Message);
            return ValidationProblem();
        }

        jobSchedulesStorage.Refresh();

        return Ok();
    }

    public class JobEditViewModel
    {
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
