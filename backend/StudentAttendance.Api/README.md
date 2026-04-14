# StudentAttendance API (C# + JSON Storage)

This backend is an ASP.NET Core Minimal API that serves your existing HTML pages and stores app data in a JSON file.

## Features

- Student register and login endpoints
- Profile read/update endpoints
- Attendance create/list endpoints
- Dashboard summary endpoint
- File-based persistence in `Data/storage.json`

## New Device Setup (Step-by-Step)

Use this section if the device is not set up yet.

1. Install prerequisites

- Install .NET 8 SDK (required): E download ni , ayg kahadlok wala ni VIRUS
   https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-8.0.419-windows-x64-installer

   
- Optional but recommended: Git and VS Code.

2. Get the project files  (OPTIONAL RANI)

- Clone with Git, or copy the `StudentAttendance` folder to the new device.

3. Open terminal in the project root folder (OPTIONAL RANI)

- Project root means the folder that contains `index.html`, `login.html`, and `backend/`.

4. Verify .NET is installed (Go to terminal, press window key then type CMD then press ENTER) 

- Run: write this command

   `dotnet --version`

- You should see a version number (8.x is recommended). (Nya dapat mo display ni)

5. Restore and build (E open ang project sa VSCODE nya pangita'a ang TERMINAL)

- Run: (igka kita sa TERMINAL, E run ni nga command, E type ra)

   `dotnet restore .\backend\StudentAttendance.Api\StudentAttendance.Api.csproj`

   `dotnet build .\backend\StudentAttendance.Api\StudentAttendance.Api.csproj`

6. Run the server

- From project root:

   `dotnet run --project .\backend\StudentAttendance.Api\StudentAttendance.Api.csproj`

- Or from API folder:

   `cd .\backend\StudentAttendance.Api`

   `dotnet run`

7. Open in browser

- Go to:

   `http://localhost:5099`

- Do not open HTML files directly with `file://...`; run through the backend URL above.

8. Stop the app

- Press `Ctrl + C` in the running terminal.

## Quick Run (Already Set Up)

1. Open a terminal in the project root.
2. Run:

    `dotnet run --project .\backend\StudentAttendance.Api\StudentAttendance.Api.csproj`

3. Open your browser at:

    `http://localhost:5099`

The app pages are served by the backend, so API calls work on the same origin.

## Troubleshooting

- `dotnet is not recognized`
   Install .NET SDK 8 and reopen terminal.

- `MSBUILD : error ... project file does not exist`
   You are likely in the wrong folder. Use the exact run command from project root above.

- `Address already in use`
   Another process is already using port 5099. Stop the old process or change the port in `Properties/launchSettings.json`.

- Frontend opens but API calls fail
   Make sure you opened `http://localhost:5099` and not a local file path.

## Data File

Stored here:

- `backend/StudentAttendance.Api/Data/storage.json`

You can inspect this file to see registered users and attendance records.
