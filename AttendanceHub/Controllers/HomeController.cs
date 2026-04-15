using Microsoft.AspNetCore.Mvc;

namespace StudentAttendance.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class HomeController : ControllerBase
{
    // Redirects the root URL to the static landing page inside Views.
    [HttpGet("/")]
    public IActionResult Index()
    {
        return Redirect("/index.html");
    }
}