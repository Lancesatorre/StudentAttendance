namespace StudentAttendance.Api;

public record RegisterRequest(
    string FullName,
    string StudentId,
    string YearLevel,
    string Course,
    string Email,
    string Password,
    string ConfirmPassword);

public record LoginRequest(string Email, string Password);

public record UpdateProfileRequest(
    string FullName,
    string YearLevel,
    string Course,
    string Email,
    string MobileNumber,
    string Address);

public record CreateAttendanceRequest(
    string UserId,
    string StudentName,
    string StudentId,
    string Date,
    string Role,
    string TimeIn,
    string TimeOut,
    string Course,
    string SubjectCode,
    string Section);

public record AuthResponse(
    string UserId,
    string FullName,
    string StudentId,
    string YearLevel,
    string Course,
    string Email);

public record UserProfileResponse(
    string UserId,
    string FullName,
    string StudentId,
    string YearLevel,
    string Course,
    string Email,
    string MobileNumber,
    string Address);

public record AttendanceResponse(
    string Id,
    string UserId,
    string StudentName,
    string StudentId,
    string Date,
    string Role,
    string TimeIn,
    string TimeOut,
    string Course,
    string SubjectCode,
    string Section,
    string Status,
    string CreatedAtUtc);

public record RecentAttendanceResponse(
    string Subject,
    string Date,
    string TimeIn,
    string Section,
    string Status);

public record SubjectRateResponse(string Subject, int Rate);

public record DashboardResponse(
    int AttendanceRate,
    int ClassesToday,
    int LateThisMonth,
    int PresentStreak,
    List<RecentAttendanceResponse> RecentRecords,
    List<SubjectRateResponse> SubjectRates);

internal sealed class UserEntity
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string YearLevel { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string CreatedAtUtc { get; set; } = string.Empty;
}

internal sealed class AttendanceEntity
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string TimeIn { get; set; } = string.Empty;
    public string TimeOut { get; set; } = string.Empty;
    public string Course { get; set; } = string.Empty;
    public string SubjectCode { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAtUtc { get; set; } = string.Empty;
}

internal sealed class DataEnvelope
{
    public List<UserEntity> Users { get; set; } = new();
    public List<AttendanceEntity> Attendance { get; set; } = new();
}
