using Microsoft.AspNetCore.Mvc;
using StudentAttendance.Api.Services;

namespace StudentAttendance.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class AttendanceController : ControllerBase
{
    private readonly JsonDataStore _store;

    public AttendanceController(JsonDataStore store)
    {
        _store = store;
    }

    // Saves one attendance entry and returns either validation errors or the created attendance record.
    [HttpPost("attendance")]
    public IActionResult CreateAttendance([FromBody] CreateAttendanceRequest request)
    {
        var createAttendanceResult = _store.AddAttendance(request);
        if (!createAttendanceResult.Success)
        {
            return BadRequest(new { message = createAttendanceResult.Error });
        }

        return Ok(createAttendanceResult.Record);
    }

    // Returns all attendance records for a given userId, or a bad request when userId is missing.
    [HttpGet("attendance")]
    public IActionResult GetAttendance([FromQuery] string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { message = "Query parameter userId is required." });
        }

        var attendanceRecords = _store.GetAttendance(userId);
        return Ok(attendanceRecords);
    }
}