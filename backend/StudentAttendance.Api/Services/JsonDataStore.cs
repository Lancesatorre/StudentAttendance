using System.Text.Json;

namespace StudentAttendance.Api.Services;

public sealed class JsonDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly object _fileLock = new();
    private readonly string _jsonFilePath;

    // Set up the Data folder and storage.json file path.
    public JsonDataStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDirectory);

        _jsonFilePath = Path.Combine(dataDirectory, "storage.json");
        CreateJsonFileIfMissing();
    }

    // Register a new student account.
    // Example: FullName="Ana Cruz", StudentId="2026-0001", Course="BSIT", Email="ana@mail.com".
    public (bool Success, string Error, AuthResponse? User) Register(RegisterRequest request)
    {
        var fullName = (request.FullName ?? string.Empty).Trim();
        var studentId = (request.StudentId ?? string.Empty).Trim();
        var yearLevel = (request.YearLevel ?? string.Empty).Trim();
        var course = (request.Course ?? string.Empty).Trim();
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = request.Password ?? string.Empty;
        var confirmPassword = request.ConfirmPassword ?? string.Empty;

        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(studentId) ||
            string.IsNullOrWhiteSpace(yearLevel) ||
            string.IsNullOrWhiteSpace(course) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return (false, "Please complete all fields.", null);
        }

        if (password != confirmPassword)
        {
            return (false, "Password and confirm password do not match.", null);
        }

        lock (_fileLock)
        {
            var database = ReadData();

            foreach (var existingUser in database.Users)
            {
                if (string.Equals(existingUser.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Email already exists.", null);
                }

                if (string.Equals(existingUser.StudentId, studentId, StringComparison.OrdinalIgnoreCase))
                {
                    return (false, "Student ID already exists.", null);
                }
            }

            var user = new UserEntity
            {
                Id = GetNextUserId(database.Users),
                FullName = fullName,
                StudentId = studentId,
                YearLevel = yearLevel,
                Course = course,
                Email = email,
                Password = password,
                CreatedAtUtc = DateTime.UtcNow.ToString("O")
            };

            database.Users.Add(user);
            WriteData(database);

            return (true, string.Empty, ToAuthResponse(user));
        }
    }

    // Check student email and password for login.
    // Example: Email="ana@mail.com", Password="123456".
    public (bool Success, string Error, AuthResponse? User) Login(LoginRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim().ToLowerInvariant();
        var password = request.Password ?? string.Empty;

        lock (_fileLock)
        {
            var database = ReadData();

            foreach (var user in database.Users)
            {
                if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(user.Password, password, StringComparison.Ordinal))
                {
                    return (true, string.Empty, ToAuthResponse(user));
                }
            }

            return (false, "Invalid email or password.", null);
        }
    }

    // Save one attendance record for a student.
    // Example: UserId="1", Date="2026-04-15", TimeIn="08:00", TimeOut="10:00".
    public (bool Success, string Error, AttendanceResponse? Record) AddAttendance(CreateAttendanceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) ||
            string.IsNullOrWhiteSpace(request.Date) ||
            string.IsNullOrWhiteSpace(request.TimeIn) ||
            string.IsNullOrWhiteSpace(request.TimeOut) ||
            string.IsNullOrWhiteSpace(request.SubjectCode) ||
            string.IsNullOrWhiteSpace(request.Section))
        {
            return (false, "Please complete all required attendance fields.", null);
        }

        lock (_fileLock)
        {
            var database = ReadData();
            UserEntity? user = null;

            foreach (var existingUser in database.Users)
            {
                if (existingUser.Id == request.UserId)
                {
                    user = existingUser;
                    break;
                }
            }

            if (user is null)
            {
                return (false, "User not found. Please login again.", null);
            }

            var attendance = new AttendanceEntity
            {
                Id = GetNextAttendanceId(database.Attendance),
                UserId = user.Id,
                StudentName = string.IsNullOrWhiteSpace(request.StudentName) ? user.FullName : request.StudentName.Trim(),
                StudentId = string.IsNullOrWhiteSpace(request.StudentId) ? user.StudentId : request.StudentId.Trim(),
                Date = request.Date.Trim(),
                Role = string.IsNullOrWhiteSpace(request.Role) ? "Student" : request.Role.Trim(),
                TimeIn = request.TimeIn.Trim(),
                TimeOut = request.TimeOut.Trim(),
                Course = string.IsNullOrWhiteSpace(request.Course) ? user.Course : request.Course.Trim(),
                SubjectCode = request.SubjectCode.Trim(),
                Section = request.Section.Trim(),
                Status = GetAttendanceStatus(request.TimeIn),
                CreatedAtUtc = DateTime.UtcNow.ToString("O")
            };

            database.Attendance.Add(attendance);
            WriteData(database);

            return (true, string.Empty, ToAttendanceResponse(attendance));
        }
    }

    // Get all attendance records of one student.
    // Example: userId="1".
    public List<AttendanceResponse> GetAttendance(string userId)
    {
        lock (_fileLock)
        {
            var database = ReadData();
            var selectedRecords = new List<AttendanceEntity>();

            foreach (var item in database.Attendance)
            {
                if (item.UserId == userId)
                {
                    selectedRecords.Add(item);
                }
            }

            selectedRecords.Sort((a, b) => string.CompareOrdinal(b.CreatedAtUtc, a.CreatedAtUtc));

            var response = new List<AttendanceResponse>();
            foreach (var item in selectedRecords)
            {
                response.Add(ToAttendanceResponse(item));
            }

            return response;
        }
    }

    // Change user entity format into API response format.
    private static AuthResponse ToAuthResponse(UserEntity user)
    {
        return new AuthResponse(user.Id, user.FullName, user.StudentId, user.YearLevel, user.Course, user.Email);
    }

    // Change attendance entity format into API response format.
    private static AttendanceResponse ToAttendanceResponse(AttendanceEntity record)
    {
        return new AttendanceResponse(
            record.Id,
            record.UserId,
            record.StudentName,
            record.StudentId,
            record.Date,
            record.Role,
            record.TimeIn,
            record.TimeOut,
            record.Course,
            record.SubjectCode,
            record.Section,
            record.Status,
            record.CreatedAtUtc);
    }

    // Decide if attendance is Present or Late using Time In.
    private static string GetAttendanceStatus(string timeInText)
    {
        if (!TimeSpan.TryParse(timeInText, out var timeIn))
        {
            return "Present";
        }

        var lateCutoff = new TimeSpan(8, 10, 0);
        return timeIn > lateCutoff ? "Late" : "Present";
    }

    // Make the next user ID using +1 increment.
    private static string GetNextUserId(List<UserEntity> users)
    {
        var highestId = 0;

        foreach (var user in users)
        {
            if (int.TryParse(user.Id, out var number) && number > highestId)
            {
                highestId = number;
            }
        }

        return (highestId + 1).ToString();
    }

    // Make the next attendance ID using +1 increment.
    private static string GetNextAttendanceId(List<AttendanceEntity> attendanceList)
    {
        var highestId = 0;

        foreach (var attendance in attendanceList)
        {
            if (int.TryParse(attendance.Id, out var number) && number > highestId)
            {
                highestId = number;
            }
        }

        return (highestId + 1).ToString();
    }

    // Create storage.json if it is missing.
    private void CreateJsonFileIfMissing()
    {
        if (File.Exists(_jsonFilePath))
        {
            return;
        }

        var emptyData = JsonSerializer.Serialize(new DataEnvelope(), SerializerOptions);
        File.WriteAllText(_jsonFilePath, emptyData);
    }

    // Read users and attendance data from storage.json.
    private DataEnvelope ReadData()
    {
        var json = File.ReadAllText(_jsonFilePath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return new DataEnvelope();
        }

        var data = JsonSerializer.Deserialize<DataEnvelope>(json, SerializerOptions);
        return data ?? new DataEnvelope();
    }

    // Save users and attendance data to storage.json.
    private void WriteData(DataEnvelope data)
    {
        var json = JsonSerializer.Serialize(data, SerializerOptions);
        File.WriteAllText(_jsonFilePath, json);
    }
}
