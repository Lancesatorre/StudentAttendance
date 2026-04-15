using Microsoft.AspNetCore.Mvc;
using StudentAttendance.Api.Services;

namespace StudentAttendance.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class AuthController : ControllerBase
{
    private readonly JsonDataStore _store;

    public AuthController(JsonDataStore store)
    {
        _store = store;
    }

    // Accepts new student registration data and returns either validation errors or the created user profile.
    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        var registerResult = _store.Register(request);
        if (!registerResult.Success)
        {
            return BadRequest(new { message = registerResult.Error });
        }

        return Ok(registerResult.User);
    }

    // Validates login credentials and returns either an error message or the authenticated user profile.
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var loginResult = _store.Login(request);
        if (!loginResult.Success)
        {
            return BadRequest(new { message = loginResult.Error });
        }

        return Ok(loginResult.User);
    }
}