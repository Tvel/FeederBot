using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FeederBot.Jobs.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeederBot.Controllers;

[Authorize]
[ApiController]
[Route("/api/[controller]")]
public class List : Controller
{
    private readonly IJobApiStorage jobApiStorage;

    public List(IJobApiStorage jobApiStorage)
    {
        this.jobApiStorage = jobApiStorage;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobListModel>>> Get()
    {
        try
        {
            IEnumerable<JobListModel> jobs = await jobApiStorage.GetAll();
            return Ok(jobs);
        }
        catch (Exception e)
        {
            ModelState.AddModelError("Job", e.Message);
            return ValidationProblem();
        }
    }
}
