using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using StudentAttendance.Api;
using StudentAttendance.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddSingleton<JsonDataStore>();

var app = builder.Build();

app.UseCors();

var staticRootPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", ".."));
var staticProvider = new PhysicalFileProvider(staticRootPath);
var frontendProvider = new PhysicalFileProvider(Path.Combine(staticRootPath, "Frontend"));

app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = frontendProvider,
    RequestPath = string.Empty
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = staticProvider,
    RequestPath = string.Empty
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = frontendProvider,
    RequestPath = string.Empty
});

app.MapGet("/", () => Results.Redirect("/index.html"));

app.MapPost("/api/auth/register", async (RegisterRequest request, JsonDataStore store) =>
{
    var result = await store.RegisterAsync(request);
    return result.Success
        ? Results.Ok(result.User)
        : Results.BadRequest(new { message = result.Error });
});

app.MapPost("/api/auth/login", async (LoginRequest request, JsonDataStore store) =>
{
    var result = await store.LoginAsync(request);
    return result.Success
        ? Results.Ok(result.User)
        : Results.BadRequest(new { message = result.Error });
});

app.MapGet("/api/users/{userId}", async (string userId, JsonDataStore store) =>
{
    var user = await store.GetUserAsync(userId);
    return user is null
        ? Results.NotFound(new { message = "User was not found." })
        : Results.Ok(user);
});

app.MapPut("/api/users/{userId}", async (string userId, UpdateProfileRequest request, JsonDataStore store) =>
{
    var result = await store.UpdateUserAsync(userId, request);
    return result.Success
        ? Results.Ok(result.User)
        : Results.BadRequest(new { message = result.Error });
});

app.MapPost("/api/attendance", async (CreateAttendanceRequest request, JsonDataStore store) =>
{
    var result = await store.AddAttendanceAsync(request);
    return result.Success
        ? Results.Ok(result.Record)
        : Results.BadRequest(new { message = result.Error });
});

app.MapGet("/api/attendance", async (string userId, JsonDataStore store) =>
{
    var records = await store.GetAttendanceAsync(userId);
    return Results.Ok(records);
});

app.MapGet("/api/dashboard/{userId}", async (string userId, JsonDataStore store) =>
{
    var dashboard = await store.GetDashboardAsync(userId);
    return dashboard is null
        ? Results.NotFound(new { message = "User was not found." })
        : Results.Ok(dashboard);
});

app.Run();
