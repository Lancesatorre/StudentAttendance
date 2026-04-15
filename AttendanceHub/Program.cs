using Microsoft.Extensions.FileProviders;
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

// Register MVC controllers.
builder.Services.AddControllers();

// Use one shared JSON data store service.
builder.Services.AddSingleton<JsonDataStore>();

// Build the app.
var app = builder.Build();

// Enable CORS middleware.
app.UseCors();

// Locate workspace root and MVC Views folder.
var projectRootPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, ".."));
var viewsPath = Path.Combine(app.Environment.ContentRootPath, "Views");

var rootFilesProvider = new PhysicalFileProvider(projectRootPath);
var viewsFilesProvider = new PhysicalFileProvider(viewsPath);

// Serve static files from project root.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = rootFilesProvider
});

// Serve static files from Views folder.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = viewsFilesProvider
});

// Map MVC controllers (API endpoints and root redirect).
app.MapControllers();

// Start the API server.
app.Run();
