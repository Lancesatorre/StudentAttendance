# JsonDataStore Explained

## Overview
`JsonDataStore` is the **data layer** of the Student Attendance app. It handles all operations on the JSON file: registration, login, profile updates, attendance submission, and dashboard calculations.

---

## What It Does

1. **Manages user accounts** (register, login, update profile)
2. **Handles attendance records** (add, retrieve, calculate stats)
3. **Reads/writes data** to `storage.json`
4. **Validates inputs** before saving
5. **Computes dashboard analytics** (attendance rate, late count, streak)

---

## Key Properties

```csharp
private static readonly JsonSerializerOptions SerializerOptions
```
- Formats JSON output with camelCase (like `userId` instead of `UserId`)
- Makes output readable and consistent

```csharp
private readonly SemaphoreSlim _syncLock = new(1, 1);
```
- Prevents concurrent file access issues
- Only one operation can read/write at a time

```csharp
private readonly string _storageFilePath
```
- Path to `storage.json` (the database file)
- Created in the `Data/` folder

---

## Main Methods

### 1. **RegisterAsync** - User Registration

**What it does:**
- Validates all required fields (name, email, student ID, etc.)
- Checks if email or student ID already exist
- Hashes the password using SHA256
- Creates a new user and saves to JSON

**Key Steps:**
1. Trim and validate inputs
2. Check for duplicate email or student ID
3. Hash password
4. Create user with unique ID (GUID)
5. Add to JSON file
6. Return success/error

**Example Flow:**
```
Input: Email=john@example.com, Password=pass123
↓
Validate fields
↓
Check duplicates
↓
Hash password → SHA256
↓
Create UserEntity with unique ID
↓
Save to storage.json
↓
Return: Success + user details
```

---

### 2. **LoginAsync** - User Login

**What it does:**
- Takes email and password
- Hashes the provided password
- Compares with stored hash
- Returns user info if match found

**Key Steps:**
1. Normalize email (lowercase)
2. Hash the provided password
3. Search for user with matching email AND password hash
4. Return user or error

**Security Note:** Passwords are never stored plain text. Only the hash is saved.

---

### 3. **GetUserAsync** - Retrieve User Profile

**What it does:**
- Fetches a full user profile by user ID
- Used when user opens the profile page

**Example:**
```
Input: userId = "abc123"
↓
Read storage.json
↓
Find user with ID = "abc123"
↓
Return UserProfileResponse with all details
```

---

### 4. **UpdateUserAsync** - Update Profile

**What it does:**
- Updates profile fields (name, email, phone, address, etc.)
- Validates new email is not already in use
- Saves changes to JSON

**Fields Updated:**
- Full Name
- Year Level
- Course
- Email
- Mobile Number
- Address

**Student ID cannot be changed** (immutable).

---

### 5. **AddAttendanceAsync** - Submit Attendance

**What it does:**
- Validates date and time formats
- Determines if attendance is "Present" or "Late"
- Creates attendance record
- Saves to JSON

**Validation:**
- Date must be `yyyy-MM-dd` format
- TimeIn and TimeOut must be valid time values
- User must be logged in

**Status Logic:**
```csharp
if (TimeIn > 08:10) → "Late"
else → "Present"
```

**Example:**
```
Input: TimeIn = 08:05
↓
Compare with cutoff 08:10
↓
08:05 is NOT > 08:10
↓
Status = "Present"
```

Another example:
```
Input: TimeIn = 08:15
↓
Compare with cutoff 08:10
↓
08:15 IS > 08:10
↓
Status = "Late"
```

---

### 6. **GetAttendanceAsync** - Retrieve Records

**What it does:**
- Fetches all attendance records for a user
- Sorts by date (newest first) and time (newest first)
- Returns list to frontend

**Used by:**
- Attendance-record page (shows full history)
- New-attendance page (shows recent submissions)

---

### 7. **GetDashboardAsync** - Calculate Dashboard Stats

**What it does:**
- Loads all attendance records for a user
- Calculates:
  - **Attendance Rate** - Percentage of "Present" entries
  - **Classes Today** - Records matching today's date
  - **Late This Month** - Count of "Late" entries this month
  - **Present Streak** - Consecutive days with "Present" status
  - **Recent Records** - Last 5 attendance entries
  - **Subject Rates** - Attendance percentage per subject

**Example Calculation:**
```
Total records = 20
Present = 18
Late = 2

Attendance Rate = (18 / 20) * 100 = 90%
```

**Streak Calculation:**
```
If dates: March 10 (Present), March 9 (Present), March 8 (Present), March 7 (Absent)

Streak = 3 (consecutive days back from today until break)
```

---

## Helper Methods

### **DetermineStatus** - Mark Present or Late

```csharp
private static string DetermineStatus(TimeSpan timeIn)
{
    var lateCutoff = new TimeSpan(8, 10, 0);
    return timeIn > lateCutoff ? "Late" : "Present";
}
```

- Compares time-in with 8:10 AM cutoff
- Simple rule: before or at 8:10 = Present, after = Late

---

### **HashPassword** - Secure Password Storage

```csharp
private static string HashPassword(string password)
{
    var bytes = Encoding.UTF8.GetBytes(password);
    var hash = SHA256.HashData(bytes);
    return Convert.ToHexString(hash);
}
```

- Converts password to bytes
- Applies SHA256 hashing algorithm
- Returns hex string (e.g., `A1B2C3D4...`)
- **Not reversible** - cannot decrypt to get original password

---

### **ParseDate** - Convert String to Date

```csharp
private static DateOnly? ParseDate(string value)
{
    return DateOnly.TryParseExact(value, "yyyy-MM-dd", ...);
}
```

- Takes `"2024-03-15"` string
- Converts to `DateOnly` object (for date math)
- Returns null if format is invalid

---

### **NormalizeTime** - Convert String to TimeSpan

```csharp
private static TimeSpan? NormalizeTime(string value)
{
    return TimeSpan.TryParse(value, ...);
}
```

- Takes `"08:30"` or `"8:30"` string
- Converts to `TimeSpan` object (for time math)
- Returns null if invalid

---

### **EnsureStorageFileExists** - Initialize JSON File

```csharp
private void EnsureStorageFileExists()
{
    if (File.Exists(_storageFilePath)) return;
    
    var emptyData = JsonSerializer.Serialize(new DataEnvelope(), ...);
    File.WriteAllText(_storageFilePath, emptyData);
}
```

- Checks if `storage.json` already exists
- If not, creates it with empty structure:
  ```json
  {
    "users": [],
    "attendance": []
  }
  ```

---

### **ReadDataUnsafeAsync** - Load JSON from Disk

```csharp
private async Task<DataEnvelope> ReadDataUnsafeAsync()
{
    await using var stream = File.Open(_storageFilePath, ...);
    var data = await JsonSerializer.DeserializeAsync<DataEnvelope>(stream, ...);
    return data ?? new DataEnvelope();
}
```

- Asynchronously opens `storage.json`
- Deserializes JSON into `DataEnvelope` object (contains users + attendance lists)
- Returns empty structure if file is empty

---

### **WriteDataUnsafeAsync** - Save JSON to Disk

```csharp
private async Task WriteDataUnsafeAsync(DataEnvelope data)
{
    await using var stream = File.Open(_storageFilePath, FileMode.Create, ...);
    await JsonSerializer.SerializeAsync(stream, data, ...);
}
```

- Asynchronously opens `storage.json` for writing
- Serializes the entire data structure back to JSON
- Overwrites the file completely

---

## Data Flow Example: Register → Login → Submit Attendance

### Step 1: Register
```
Frontend Form
  ↓ (POST /api/auth/register)
Program.cs Route
  ↓
RegisterAsync()
  ├─ Validate inputs
  ├─ Check duplicates
  ├─ HashPassword("pass123") → "A1B2C3..."
  ├─ ReadDataUnsafeAsync()
  ├─ Create UserEntity
  ├─ WriteDataUnsafeAsync() ← Saves to storage.json
  └─ Return success
  ↓
Frontend redirects to login
```

### Step 2: Login
```
Frontend Form
  ↓ (POST /api/auth/login)
Program.cs Route
  ↓
LoginAsync()
  ├─ HashPassword("pass123") → "A1B2C3..."
  ├─ ReadDataUnsafeAsync()
  ├─ Find user where email matches AND hash matches
  └─ Return user details
  ↓
Frontend stores user in localStorage
Frontend redirects to dashboard
```

### Step 3: Submit Attendance
```
Frontend Form
  ↓ (POST /api/attendance)
Program.cs Route
  ↓
AddAttendanceAsync()
  ├─ ValidateDate("2024-03-15") ✓
  ├─ ValidateTime("08:30") ✓
  ├─ ReadDataUnsafeAsync()
  ├─ Find user by ID ✓
  ├─ DetermineStatus(08:30) → "Present"
  ├─ Create AttendanceEntity
  ├─ WriteDataUnsafeAsync() ← Saves to storage.json
  └─ Return record
  ↓
Frontend displays success message
Frontend reloads recent records
```

---

## Storage Structure (storage.json)

```json
{
  "users": [
    {
      "id": "abc123",
      "fullName": "John Doe",
      "studentId": "2024001",
      "yearLevel": "2nd Year",
      "course": "BS Computer Science",
      "email": "john@example.com",
      "passwordHash": "A1B2C3D4E5F6...",
      "mobileNumber": "09123456789",
      "address": "123 Main St",
      "createdAtUtc": "2024-03-01T10:30:00Z"
    }
  ],
  "attendance": [
    {
      "id": "record001",
      "userId": "abc123",
      "studentName": "John Doe",
      "studentId": "2024001",
      "date": "2024-03-15",
      "role": "Student",
      "timeIn": "08:05",
      "timeOut": "09:00",
      "course": "BS Computer Science",
      "subjectCode": "CS-301",
      "section": "A1",
      "status": "Present",
      "createdAtUtc": "2024-03-15T08:05:00Z"
    }
  ]
}
```

---

## Summary

| Method | Purpose | Used By |
|--------|---------|---------|
| `RegisterAsync` | Create new account | Register page |
| `LoginAsync` | Verify credentials | Login page |
| `GetUserAsync` | Fetch profile | Profile page |
| `UpdateUserAsync` | Edit profile | Profile page |
| `AddAttendanceAsync` | Save attendance | New-attendance page |
| `GetAttendanceAsync` | Load history | Attendance-record page, New-attendance page |
| `GetDashboardAsync` | Calculate stats | Dashboard page |

**Key Takeaway:**
- JsonDataStore is the **single point of truth** for all data operations
- It validates, processes, and persists everything
- All frontend requests funnel through here
- The JSON file is the "database"
