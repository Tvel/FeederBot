using System;
using System.Threading.Tasks;
using FeederBot.Jobs;
using FeederBot.Jobs.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeederBot.Controllers;

[Authorize]
[ApiController]
[Route("/api/[controller]")]
public class Delete : Controller
{
    private readonly IJobApiStorage jobApiStorage;
    private readonly JobSchedulesStorage jobSchedulesStorage;

    public Delete(IJobApiStorage jobApiStorage, JobSchedulesStorage jobSchedulesStorage)
    {
        this.jobApiStorage = jobApiStorage;
        this.jobSchedulesStorage = jobSchedulesStorage;
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> Index([FromRoute] string id)
    {
        try
        {
            await jobApiStorage.Delete(id);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("Job", e.Message);
            return ValidationProblem();
        }

        jobSchedulesStorage.Refresh();

        return Ok();
    }

}
