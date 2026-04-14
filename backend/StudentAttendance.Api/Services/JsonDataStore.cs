using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace StudentAttendance.Api.Services;

public sealed class JsonDataStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private readonly string _storageFilePath;

    public JsonDataStore(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "Data");
        Directory.CreateDirectory(dataDirectory);

        _storageFilePath = Path.Combine(dataDirectory, "storage.json");
        EnsureStorageFileExists();
    }

    public async Task<(bool Success, string Error, AuthResponse? User)> RegisterAsync(RegisterRequest request)
    {
        var fullName = request.FullName.Trim();
        var studentId = request.StudentId.Trim();
        var yearLevel = request.YearLevel.Trim();
        var course = request.Course.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(studentId) ||
            string.IsNullOrWhiteSpace(yearLevel) ||
            string.IsNullOrWhiteSpace(course) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return (false, "All required fields must be provided.", null);
        }

        if (request.Password != request.ConfirmPassword)
        {
            return (false, "Password and confirm password do not match.", null);
        }

        await _syncLock.WaitAsync();
        try
        {
            var data = await ReadDataUnsafeAsync();

            if (data.Users.Any(user => string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "An account with this email already exists.", null);
            }

            if (data.Users.Any(user => string.Equals(user.StudentId, studentId, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "An account with this student ID already exists.", null);
            }

            var entity = new UserEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                FullName = fullName,
                StudentId = studentId,
                YearLevel = yearLevel,
                Course = course,
                Email = email,
                PasswordHash = HashPassword(request.Password),
                MobileNumber = string.Empty,
                Address = string.Empty,
                CreatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };

            data.Users.Add(entity);
            await WriteDataUnsafeAsync(data);

            return (true, string.Empty, ToAuthResponse(entity));
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<(bool Success, string Error, AuthResponse? User)> LoginAsync(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var passwordHash = HashPassword(request.Password);

        await _syncLock.WaitAsync();
        try
        {
            var data = await ReadDataUnsafeAsync();
            var matchedUser = data.Users.FirstOrDefault(user =>
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(user.PasswordHash, passwordHash, StringComparison.Ordinal));

            if (matchedUser is null)
            {
                return (false, "Invalid email or password.", null);
            }

            return (true, string.Empty, ToAuthResponse(matchedUser));
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<UserProfileResponse?> GetUserAsync(string userId)
    {
        await _syncLock.WaitAsync();
        try
        {
            var data = await ReadDataUnsafeAsync();
            var user = data.Users.FirstOrDefault(item => item.Id == userId);
            return user is null
                ? null
                : new UserProfileResponse(
                    user.Id,
                    user.FullName,
                    user.StudentId,
                    user.YearLevel,
                    user.Course,
                    user.Email,
                    user.MobileNumber,
                    user.Address);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<(bool Success, string Error, UserProfileResponse? User)> UpdateUserAsync(string userId, UpdateProfileRequest request)
    {
        await _syncLock.WaitAsync();
        try
        {
            var data = await ReadDataUnsafeAsync();
            var user = data.Users.FirstOrDefault(item => item.Id == userId);

            if (user is null)
            {
                return (false, "User was not found.", null);
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();

            if (data.Users.Any(item => item.Id != userId && string.Equals(item.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                return (false, "Another account is already using this email.", null);
            }

            user.FullName = request.FullName.Trim();
            user.YearLevel = request.YearLevel.Trim();
            user.Course = request.Course.Trim();
            user.Email = normalizedEmail;
            user.MobileNumber = request.MobileNumber.Trim();
            user.Address = request.Address.Trim();

            await WriteDataUnsafeAsync(data);

            return (true, string.Empty, new UserProfileResponse(
                user.Id,
                user.FullName,
                user.StudentId,
                user.YearLevel,
                user.Course,
                user.Email,
                user.MobileNumber,
                user.Address));
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<(bool Success, string Error, AttendanceResponse? Record)> AddAttendanceAsync(CreateAttendanceRequest request)
    {
        var parsedDate = ParseDate(request.Date);
        if (parsedDate is null)
        {
            return (false, "Date must be in yyyy-MM-dd format.", null);
        }

        var timeIn = NormalizeTime(request.TimeIn);
        var timeOut = NormalizeTime(request.TimeOut);
        if (timeIn is null || timeOut is null)
        {
            return (false, "Time-in and time-out must be valid times.", null);
        }

        await _syncLock.WaitAsync();
        try
        {
            var data = await ReadDataUnsafeAsync();
            var user = data.Users.FirstOrDefault(item => item.Id == request.UserId);
            if (user is null)
            {
                return (false, "Please login before submitting attendance.", null);
            }

            var record = new AttendanceEntity
            {
                Id = Guid.NewGuid().ToString("N"),
                UserId = user.Id,
                StudentName = string.IsNullOrWhiteSpace(request.StudentName) ? user.FullName : request.StudentName.Trim(),
                StudentId = string.IsNullOrWhiteSpace(request.StudentId) ? user.StudentId : request.StudentId.Trim(),
                Date = parsedDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Role = request.Role.Trim(),
                TimeIn = timeIn.Value.ToString("hh\\:mm", CultureInfo.InvariantCulture),
                TimeOut = timeOut.Value.ToString("hh\\:mm", CultureInfo.InvariantCulture),
                Course = string.IsNullOrWhiteSpace(request.Course) ? user.Course : request.Course.Trim(),
                SubjectCode = request.SubjectCode.Trim(),
                Section = request.Section.Trim(),
                Status = DetermineStatus(timeIn.Value),
                CreatedAtUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };

            data.Attendance.Add(record);
            await WriteDataUnsafeAsync(data);

            return (true, string.Empty, ToAttendanceResponse(record));
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<List<AttendanceResponse>> GetAttendanceAsync(string userId)
    {
        await _syncLock.WaitAsync();
        try
        {
            var data = await ReadDataUnsafeAsync();
            return data.Attendance
                .Where(item => item.UserId == userId)
                .OrderByDescending(item => ParseDate(item.Date) ?? DateOnly.MinValue)
                .ThenByDescending(item => NormalizeTime(item.TimeIn) ?? TimeSpan.Zero)
                .Select(ToAttendanceResponse)
                .ToList();
        }
        finally
        {
            _syncLock.Release();
        }
    }

    public async Task<DashboardResponse?> GetDashboardAsync(string userId)
    {
        await _syncLock.WaitAsync();
        try
        {
            var data = await ReadDataUnsafeAsync();
            if (data.Users.All(user => user.Id != userId))
            {
                return null;
            }

            var records = data.Attendance.Where(item => item.UserId == userId).ToList();
            var totalCount = records.Count;
            var presentCount = records.Count(item => item.Status.Equals("Present", StringComparison.OrdinalIgnoreCase));
            var attendanceRate = totalCount == 0
                ? 0
                : (int)Math.Round(presentCount * 100d / totalCount, MidpointRounding.AwayFromZero);

            var today = DateOnly.FromDateTime(DateTime.Now);
            var thisMonth = today.Month;
            var thisYear = today.Year;

            var classesToday = records.Count(item => ParseDate(item.Date) == today);
            var lateThisMonth = records.Count(item =>
            {
                var recordDate = ParseDate(item.Date);
                return recordDate is not null
                    && recordDate.Value.Month == thisMonth
                    && recordDate.Value.Year == thisYear
                    && item.Status.Equals("Late", StringComparison.OrdinalIgnoreCase);
            });

            var presentDates = records
                .Where(item => item.Status.Equals("Present", StringComparison.OrdinalIgnoreCase))
                .Select(item => ParseDate(item.Date))
                .Where(item => item is not null)
                .Select(item => item!.Value)
                .ToHashSet();

            var streakDate = today;
            var streak = 0;
            while (presentDates.Contains(streakDate))
            {
                streak++;
                streakDate = streakDate.AddDays(-1);
            }

            var recentRecords = records
                .OrderByDescending(item => ParseDate(item.Date) ?? DateOnly.MinValue)
                .ThenByDescending(item => NormalizeTime(item.TimeIn) ?? TimeSpan.Zero)
                .Take(5)
                .Select(item => new RecentAttendanceResponse(
                    item.SubjectCode,
                    item.Date,
                    item.TimeIn,
                    item.Section,
                    item.Status))
                .ToList();

            var subjectRates = records
                .GroupBy(item => item.SubjectCode)
                .OrderBy(group => group.Key)
                .Select(group =>
                {
                    var groupTotal = group.Count();
                    var groupPresent = group.Count(item => item.Status.Equals("Present", StringComparison.OrdinalIgnoreCase));
                    var rate = groupTotal == 0 ? 0 : (int)Math.Round(groupPresent * 100d / groupTotal, MidpointRounding.AwayFromZero);
                    return new SubjectRateResponse(group.Key, rate);
                })
                .ToList();

            return new DashboardResponse(attendanceRate, classesToday, lateThisMonth, streak, recentRecords, subjectRates);
        }
        finally
        {
            _syncLock.Release();
        }
    }

    private static AuthResponse ToAuthResponse(UserEntity user)
    {
        return new AuthResponse(user.Id, user.FullName, user.StudentId, user.YearLevel, user.Course, user.Email);
    }

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

    private static string DetermineStatus(TimeSpan timeIn)
    {
        // A simple attendance rule: entries after 08:10 are marked late.
        var lateCutoff = new TimeSpan(8, 10, 0);
        return timeIn > lateCutoff ? "Late" : "Present";
    }

    private static string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    private static DateOnly? ParseDate(string value)
    {
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static TimeSpan? NormalizeTime(string value)
    {
        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var time)
            ? time
            : null;
    }

    private void EnsureStorageFileExists()
    {
        if (File.Exists(_storageFilePath))
        {
            return;
        }

        var emptyData = JsonSerializer.Serialize(new DataEnvelope(), SerializerOptions);
        File.WriteAllText(_storageFilePath, emptyData);
    }

    private async Task<DataEnvelope> ReadDataUnsafeAsync()
    {
        await using var stream = File.Open(_storageFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var data = await JsonSerializer.DeserializeAsync<DataEnvelope>(stream, SerializerOptions);
        return data ?? new DataEnvelope();
    }

    private async Task WriteDataUnsafeAsync(DataEnvelope data)
    {
        await using var stream = File.Open(_storageFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, data, SerializerOptions);
    }
}
