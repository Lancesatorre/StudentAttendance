using Microsoft.Extensions.FileProviders;
using StudentAttendance.Api;
using StudentAttendance.Api.Services;

// Create web application builder.
var builder = WebApplication.CreateBuilder(args);

// Allow frontend requests from any origin for this beginner project.
builder.Services.AddCors(corsOptions =>
{
    corsOptions.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder.AllowAnyOrigin();
        policyBuilder.AllowAnyHeader();
        policyBuilder.AllowAnyMethod();
    });
});

// Use one shared JSON data store service.
builder.Services.AddSingleton<JsonDataStore>();

// Build the app.
var app = builder.Build();

// Enable CORS middleware.
app.UseCors();

// Locate project root and Frontend folder.
var projectRootPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", ".."));
var frontendPath = Path.Combine(projectRootPath, "Frontend");

var rootFilesProvider = new PhysicalFileProvider(projectRootPath);
var frontendFilesProvider = new PhysicalFileProvider(frontendPath);

// Serve static files from project root.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = rootFilesProvider
});

// Serve static files from Frontend folder.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = frontendFilesProvider
});

// Open home page when visiting root URL.
app.MapGet("/", () => Results.Redirect("/index.html"));

// Register endpoint.
app.MapPost("/api/register", (RegisterRequest request, JsonDataStore store) =>
{
    var registerResult = store.Register(request);
    if (!registerResult.Success)
    {
        return Results.BadRequest(new { message = registerResult.Error });
    }

    return Results.Ok(registerResult.User);
});

// Login endpoint.
app.MapPost("/api/login", (LoginRequest request, JsonDataStore store) =>
{
    var loginResult = store.Login(request);
    if (!loginResult.Success)
    {
        return Results.BadRequest(new { message = loginResult.Error });
    }

    return Results.Ok(loginResult.User);
});

// Create attendance endpoint.
app.MapPost("/api/attendance", (CreateAttendanceRequest request, JsonDataStore store) =>
{
    var createAttendanceResult = store.AddAttendance(request);
    if (!createAttendanceResult.Success)
    {
        return Results.BadRequest(new { message = createAttendanceResult.Error });
    }

    return Results.Ok(createAttendanceResult.Record);
});

// Get attendance records by user ID.
app.MapGet("/api/attendance", (string userId, JsonDataStore store) =>
{
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.BadRequest(new { message = "Query parameter userId is required." });
    }

    var attendanceRecords = store.GetAttendance(userId);
    return Results.Ok(attendanceRecords);
});

// Start the API server.
app.Run();
