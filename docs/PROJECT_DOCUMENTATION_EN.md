# School Management System Documentation

> This document describes the current project after its migration to ASP.NET Core Identity, domain-wide GUID identifiers, role separation, ownership authorization, Redis caching, and load-test data seeding.

| Document field | Value |
|---|---|
| Version | 2.0 |
| Last updated | August 15, 2026 |
| Scope | Architecture, security, data, operations, Redis, seeders, and API contracts |
| Status | Ready for presentation and technical handover |

## 1. Overview

This is an ASP.NET Core MVC school-management application covering schools, managers, teachers, students, classes, subjects, assignments, grades, attendance, profiles, images, Excel exports, certificates, and error logs.

Core technologies:

- .NET 9 and ASP.NET Core MVC.
- Entity Framework Core 9 with SQL Server.
- ASP.NET Core Identity with `Guid` keys.
- Redis through `IDistributedCache` and StackExchange.Redis.
- Razor Views, Bootstrap, jQuery, and DataTables.
- EPPlus for Excel export.
- QuestPDF with Amiri fonts for Arabic certificates.
- MailKit for email and AspNetCoreHero.Notyf for notifications.

## 2. Repository Structure

| Path | Responsibility |
|---|---|
| `Program.cs` | DI, Identity, Redis, Session, middleware, migrations, and seeders |
| `Data/` | DbContext, domain entities, ApplicationUser, and seeders |
| `Controllers/` | MVC page delivery; grade and attendance controllers retain page-opening actions only |
| `Controllers/ApiController/` | JSON/AJAX reads and writes, exports, and diagnostics |
| `Models/` | View models and request/response models |
| `Filters/` | Role and resource-ownership enforcement |
| `Services/` | Accounts, email, compatibility validation, logging, certificates |
| `Middlewares/` | Exception capture and logging |
| `Helpers/` | GUID Session helpers |
| `Views/` | Razor UI grouped by controller |
| `Migrations/` | Initial migration, snapshot, and SQL script |
| `wwwroot/` | Front-end assets and Amiri fonts |
| `PrivateImages/` | Private images served through a controller |

## 3. Application Startup

1. Load configuration and optional `appsetting.env`.
2. Register SQL Server DbContext.
3. Configure Identity, cookies, password policy, and lockout.
4. Register compatibility Session; it is not an authorization source.
5. Register Redis, application services, and global filters.
6. Call `MigrateAsync` against the current environment database.
7. Create missing roles.
8. Seed only the main Admin when `SeedAdmin:Enabled=true` and secure settings are complete.
9. Run the load-test seeder only in Development and only when `LoadTestSeed:Enabled=true`.
10. build the middleware pipeline and conventional route.

Default route:

```text
{controller=Home}/{action=Index}/{id?}
```

## 4. Database and Identifiers

All domain primary and foreign keys use `Guid`/`Guid?`. Identifiers are no longer encrypted in URLs.

Intentional integer values are not record identifiers:

- National `IdNumber` values.
- Grade values and totals.
- Grade/class/section numbers.
- Identity-internal counters such as claim record IDs.

### Main tables

| Entity | Purpose |
|---|---|
| `AspNetUsers` | Identity users with GUID keys and `IsActive` |
| `AspNetRoles` | Admin, Manager, Teacher, Student |
| `Menegar` | Admin/manager profile, optional one-to-one user link |
| `Teacher` | Teacher profile, user and school link |
| `Student` | Student profile, user, school, and class link |
| `School` | School status, gender/type, stage, and class bounds |
| `StatusSchool` | School operational status |
| `Gender` | School type |
| `StageClass` | Stage; its unique Code is one character |
| `Branch` | Branch; BranchCode is one character |
| `TheClass` | Class/section and its school, stage, and branch |
| `Lectuer` | Subject (the historical code spelling is retained) |
| `TeacherLectuerClass` | Teacher-to-subject-to-class assignment |
| `StudentLectuerTeacher` | Student-to-subject-to-teacher assignment |
| `Grade` | Student assessment for subject/teacher/class/school |
| `Attendance` | Student attendance for subject/teacher/class/school |
| `ProfileImage` | Profile image metadata/path |
| `ErrorLog` | Persisted application exceptions |

`ApplicationUserId` is nullable and uniquely indexed in Menegar, Teacher, and Student, implementing one-to-one profiles while keeping Identity separate from domain data.

Many entities use soft-delete flags. Queries must consistently exclude logically deleted records.

## 5. Identity and Authentication

```csharp
ApplicationUser : IdentityUser<Guid>
IdentityRole<Guid>
SystemSchoolDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
```

Identity owns login, logout, registration/linking, password changes, forgotten-password flows, reset tokens, users, and roles.

Password policy:

- Minimum 10 characters.
- Digit, lowercase, uppercase, and non-alphanumeric character required.
- Unique email.
- Lockout after five failures for 15 minutes.

The Identity cookie is HttpOnly, Essential, always Secure, SameSite=Lax, valid for 30 minutes, and sliding.

`ApplicationClaimsPrincipalFactory` adds an `active` claim. Users with `IsActive=false` cannot access protected endpoints. Sign out and back in after changing roles or active state to refresh the cookie.

## 6. Roles and Authorization

| Role | Scope |
|---|---|
| `Admin` | Top-level administration, schools, school statuses, error logs |
| `Manager` | One school's data only |
| `Teacher` | Own profile, classes, subjects, assigned students, grades, attendance |
| `Student` | Own profile, subjects, grades, and attendance |

`RoleNames.Normalize` centralizes spelling and maps the legacy `menegar` value to Manager.

### Secure by default

Both DefaultPolicy and FallbackPolicy require an authenticated user with `active=true`. Actions are private unless explicitly marked `[AllowAnonymous]`. Public exceptions include login, password recovery, and intended public Home pages.

### Role checks

`AuthorizeRolesAttribute` evaluates Identity roles after normalization. It never trusts the role stored in Session.

### Resource ownership

The global `OwnershipAuthorizationFilter` provides defense in depth:

- Top-level Admin bypasses tenant ownership checks.
- Teacher IDs must match the current teacher profile.
- A teacher may access a student only when assigned to that student.
- Grade and Attendance IDs are checked against the current teacher/student.
- Student IDs must match the current student profile.
- Manager school, teacher, student, class, and subject IDs must belong to the manager's school.

Controllers also retain scoped EF queries and `SessionValidatorService`. Every new endpoint must have both an appropriate role and a resource/tenant scope check.

## 7. Session Compatibility

Legacy controllers temporarily receive values such as `Id`, `School`, `Role`, `UserName`, and `Name`. `SessionGuidExtensions` stores GUID values in canonical string form. Session is display/compatibility state, never the source of authorization.

## 8. Controller Responsibilities

- `AccountController`: authentication, profile linking, recovery, password changes.
- `HomeController`: public pages and administration dashboard.
- `ProfileController`: current user's linked profile only.
- `SchoolController`, `StatusSchoolController`: top Admin system administration.
- `MenegarController`: manager views for school students, teachers, and classes.
- `TeacherController`: teacher dashboard, students, assignments, certificate.
- `StudentController`: student dashboard and certificate; chart data comes from `StudentApiController`.
- `TheClassController`: classes and teacher assignment.
- `LectuerController`: subjects and teacher/student links.
- `GradesController`: grade pages; reads and all writes are handled by `GradesApiController`.
- `AttendanceController`: attendance pages; reads and all writes are handled by `AttendanceApiController`.
- `ExportDataController`: Excel exports.
- `ImageController`, `ImageProfileController`: safe private image retrieval/upload.
- `ErrorLogsController`: Admin-only error-log UI.
- `ApiController/`: AJAX/JSON equivalents and Redis diagnostics.

## 9. CSRF and Security Controls

- `AutoValidateAntiforgeryTokenAttribute` is global for unsafe HTTP methods.
- Sensitive forms also use explicit antiforgery validation where appropriate.
- GUIDs reduce enumeration but do not replace authorization.
- Private-image filenames reject `..` and path traversal.
- Secrets belong in User Secrets/environment variables, not committed settings.
- The former EncryptionHelper and EncryptionSettings key have been removed.
- Public registration cannot create a top-level Admin; Admin is provisioned through the protected seeder.

## 10. Redis Caching

Current configuration:

```text
localhost:6379
InstanceName: SchoolApp_
```

Primary cached collections:

```text
SchoolApp_Students_School_{SchoolGuid}
SchoolApp_Teachers_School_{SchoolGuid}
```

Relevant mutations invalidate these keys. List responses include:

- `X-Cache: MISS` when SQL was queried and Redis populated.
- `X-Cache: HIT` when Redis supplied the result.
- `X-Cache-Key` with the logical key.

Admin-only diagnostics:

```text
GET /api/diagnostics/redis
```

It returns connectivity, key count, type, TTL, and byte size. Use this endpoint or RedisInsight when `redis-cli` is unavailable.

## 11. Seeders

### Main Admin

`IdentityDataSeeder` creates or updates the top-level Admin and a Menegar profile without a school.

```powershell
dotnet user-secrets set "SeedAdmin:Password" "<strong-password>"
```

### Load-test dataset

`LoadTestDataSeeder` is blocked in code outside Development. In Development it remains opt-in through `LoadTestSeed:Enabled=true` and creates related data using `AddRange` and staged saves rather than UserManager calls per account:

- Schools, managers, teachers, classes, subjects, and students.
- Real Identity accounts and role rows.
- Teacher/class/subject and student/teacher/subject links.
- Grades and attendance rows.

Usernames:

- `manager1`, `manager2`, ...
- `teacher1`, `teacher2`, ...
- `stu1`, `stu2`, ...

Enable it with:

```powershell
dotnet user-secrets set "LoadTestSeed:Password" "LoadTest2026!Aa"
dotnet user-secrets set "LoadTestSeed:Enabled" "true"
dotnet run -- --seed-only
```

Stop `dotnet watch` before running the command. It runs as a standalone manual database-loading process, prints every batch's progress, and exits when complete. Disable `LoadTestSeed:Enabled` afterward and start the site normally.

Defaults: three schools, two managers/school, 30 teachers, 12 classes, eight subjects, 1,000 students, and five attendance days. It skips each completed `LoadTest School N`. Every batch commits independently so SQL Server can truncate/reuse its transaction log; links and grades use 500-row batches and attendance uses 250-row batches. The school remains `IsDeleted=true` while building and is activated only after completion. If execution is interrupted, the seeder inspects the incomplete school and inserts only missing links, grades, and attendance rows without deleting or duplicating existing records.

Schema constraint note: StageClass.Code and Branch.BranchCode are one character; the seeder uses `L`.

## 12. Setup and Operation

Requirements:

- A .NET SDK supporting net9.0.
- SQL Server matching the configured connection string.
- Redis at localhost:6379.

Common commands:

```powershell
dotnet restore
dotnet build
dotnet watch
dotnet ef migrations list
dotnet ef migrations has-pending-model-changes
dotnet ef database update
```

Application startup applies migrations automatically in every environment; back up the database and review migrations before a Production deployment. Never run `database drop --force` against data that has not been explicitly verified as disposable.

## 13. Migrations

`InitialGuidIdentity` creates the complete GUID/Identity schema. `AddAttendanceQueryIndex` optimizes attendance queries. `ConvertAttendanceExcuseToNvarcharMax` changes the legacy SQL `text` excuse column to searchable and sortable `nvarchar(max)` without narrowing existing data. `AddDirectorates` introduces directorates. `AddMinistriesTransfersAndAssignments` adds ministries, association tables, transfer requests, and school classifications. `BackfillOrganizationAssignments` adds identity-number lookup for transfers and migrates associations only when upgrading existing data. These migrations seed no test data into a clean database.

```powershell
dotnet ef migrations add DescriptiveName
dotnet ef database update
```

Review production migrations and back up the database before applying them.

## 14. Errors and Logging

`ErrorHandlingMiddleware` catches exceptions and `ErrorLoggerService` persists details in ErrorLog. Development uses the developer exception page; Production uses `/Home/Error` and HSTS.

The startup message `Database initialization failed` can represent migration, role creation, or main-Admin seeding failures; in Development it can also represent a load-test seeder failure. Always inspect the inner exception.

## 15. Exports, Certificates, and Images

- EPPlus generates student, teacher, and grade workbooks.
- QuestPDF and Amiri fonts generate Arabic certificates. A teacher certificate lists every distinct assigned subject on one page and changes the Arabic singular/plural label automatically. Teacher and student certificates enforce profile ownership or manager-school scope.
- Private images are outside direct static serving and are returned through an authenticated controller after filename validation.

## 16. Testing and Load Testing

The project builds with zero errors. Existing nullable warnings remain, and MailKit 4.10.0/ImageSharp 3.1.8 currently produce security advisories; schedule tested upgrades.

There is not yet a broad automated test suite. Minimum release checks:

1. Authenticate as every role.
2. Attempt teacher access to another teacher/unassigned student; expect 403.
3. Attempt student access to another student; expect 403.
4. Attempt Manager access to another school; expect 403.
5. Validate CSRF rejection.
6. Verify Redis MISS followed by HIT.
7. Run k6 or equivalent with anonymous traffic and real logins.

Track requests/sec, p95/p99 latency, 4xx/5xx rate, SQL connections, CPU/RAM, Redis hit ratio, login latency, lockouts, and connection-pool saturation.

## 17. New Endpoint Checklist

1. Use GUID domain keys.
2. Deliberately add `[AuthorizeRoles(...)]` or `[AllowAnonymous]`.
3. Scope resources by user/school; never trust a GUID alone.
4. Do not use Session as authorization state.
5. Prefer narrow request/view models over broad entity binding.
6. Apply CSRF to state-changing browser requests.
7. Invalidate affected Redis keys.
8. Add indexes for repeated filters/orderings.
9. Add a migration and positive/negative authorization tests.
10. Never log passwords, tokens, secrets, or connection strings.

## 18. Troubleshooting

- **AspNetRoles already exists:** old schema and migration history are inconsistent; recreate only a verified disposable test DB.
- **No Redis keys:** first open the manager list endpoint; first request is MISS, second should be HIT.
- **403 after an auth update:** sign out/in to refresh active and role claims.
- **Main Admin seeder does not run:** verify `SeedAdmin:Enabled` and secure account/password settings. **Load-test seeder does not run:** the environment must be Development and `LoadTestSeed:Enabled=true`.
- **String truncation:** inspect EF MaxLength, especially one-character codes.
- **Cookies fail over HTTP:** Cookie SecurePolicy is Always; use the HTTPS launch URL.

## 19. Production Checklist

- Load-test seeding is blocked outside Development; still keep `LoadTestSeed:Enabled=false` or omit the section entirely from Production configuration.
- Move SQL, SMTP, Redis, and all secrets to a secure secret store.
- Configure Redis through configuration rather than a hard-coded localhost value.
- Use TLS and a trusted reverse proxy.
- Upgrade packages with security advisories.
- Add rate limiting to login, recovery, and expensive APIs.
- Add SQL/Redis health checks, integration tests, centralized monitoring, and backup/restore procedures.

## 20. API Reference

### 20.1 Conventions and response contracts

- Development origin: `http://localhost:1908`. Use the active environment origin in deployed clients.
- Authentication uses the ASP.NET Core Identity application cookie. Browser calls send `credentials: 'same-origin'`.
- `POST`, `PUT`, `PATCH`, and `DELETE` requests send the antiforgery value in the `RequestVerificationToken` header.
- Domain identifiers are canonical GUID strings: `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`.
- DataTables sends `draw`, `start`, `length`, `search[value]`, `order[0][column]`, and `order[0][dir]`; modern endpoints cap pages at 100 records.

Standard DataTables response:

```json
{ "draw": 1, "recordsTotal": 250, "recordsFiltered": 18, "data": [] }
```

Typical successful write response:

```json
{
  "success": true,
  "message": "Saved successfully.",
  "redirectUrl": "/Attendance/ViewAttendance?teacherId=..."
}
```

| Status | Meaning |
|---|---|
| `200` | Successful read, update, or delete |
| `400` | Invalid model, GUID, or antiforgery token |
| `401` | No valid authentication context |
| `403` | Role or resource-ownership check failed |
| `404` | Resource not found inside the caller's authorized scope |
| `409` | Logical or duplicate conflict when returned by an endpoint |
| `500` | Logged internal error; no stack trace is returned |

### 20.2 Attendance API

| Method | Route | Role | Purpose and response |
|---|---|---|---|
| GET | `/api/Attendance/teacher-records?teacherId={guid}` | Teacher | DataTables records: student, class, subject, status, date, excuse, and record `id`. |
| GET | `/api/Attendance/subjects?teacherId={guid}` | Teacher | Active assignments as `[{ id, name }]`. |
| GET | `/api/Attendance/classes?teacherId={guid}&subjectId={guid}` | Teacher | Active assigned classes as `[{ id, name }]`. |
| GET | `/api/Attendance/student-summary?studentid={guid}` | Admin/Manager/Student | DataTables grouped by subject and teacher: `teacherId`, `teacherName`, `lectuerId`, `lectuerName`, `attendanceDays`, `totalDays`. |
| GET | `/api/Attendance/student-details?studentid={guid}&teacherId={guid}&lectuerId={guid}` | Admin/Manager/Student | DataTables details: `id`, `dateAndTime`, `attendanceStatus`, `excuse`. |
| GET | `/api/Attendance/student-records?studentid={guid}` | Admin/Manager/Student | Legacy detailed list retained for compatibility; current UI uses summary/details. |
| POST | `/api/Attendance/records` | Teacher | Creates or updates today's batch; returns success/message/redirectUrl. |
| PUT | `/api/Attendance/records/{id}` | Teacher | Updates `status` and `excuse` on an owned record. |
| DELETE | `/api/Attendance/records/{id}` | Teacher | Deletes an owned record. |

Batch attendance request:

```json
{
  "teacherId": "00000000-0000-0000-0000-000000000000",
  "lectuerId": "00000000-0000-0000-0000-000000000000",
  "classId": "00000000-0000-0000-0000-000000000000",
  "items": [
    { "studentId": "00000000-0000-0000-0000-000000000000", "status": "1", "excuse": null }
  ]
}
```

Allowed statuses are `1` (present), `0` (absent), and `m` (excused). The batch limit is 500; duplicate students are rejected. The server matches `teacherId` to Identity and validates school, assignment, class, subject, and every student before writing.

### 20.3 Grades API

| Method | Route | Role | Purpose and response |
|---|---|---|---|
| GET | `/api/Grades/teacher-records?teacherId={guid}` | Teacher | DataTables records for the teacher's students. |
| GET | `/api/Grades/student-records?studentid={guid}` | Admin/Manager/Student | DataTables student grades: subject, components, and computed total. |
| GET | `/api/Grades/subjects?teacherId={guid}` | Teacher | Active subjects as `[{ id, name }]`. |
| GET | `/api/Grades/classes?teacherId={guid}&subjectId={guid}` | Teacher | Active classes for the assignment. |
| POST | `/api/Grades/records` | Teacher | Batch upsert; returns success/message/redirectUrl. |
| PUT | `/api/Grades/records/{id}` | Teacher | Updates owned grade components. |
| DELETE | `/api/Grades/records/{id}` | Teacher | Deletes an owned grade record. |

Batch grade request:

```json
{
  "teacherId": "00000000-0000-0000-0000-000000000000",
  "lectuerId": "00000000-0000-0000-0000-000000000000",
  "classId": "00000000-0000-0000-0000-000000000000",
  "items": [{
    "studentId": "00000000-0000-0000-0000-000000000000",
    "firstMonth": 20, "mid": 30, "secondMonth": 20,
    "activity": 10, "final": 20
  }]
}
```

Each component accepts `null` or 0-100; null is stored as zero. School, assignment, class, subject, and every student are validated before commit.

### 20.4 Student dashboard API

| Method | Route | Role | Response |
|---|---|---|---|
| GET | `/api/student/grade-chart?idStudent={guid}` | Student | `{ lectuerName, totalGrade }[]`; `totalGrade` is the average total when multiple subject rows exist. |
| GET | `/api/student/attendance-chart?idStudent={guid}` | Student | Per subject: `{ subjectName, totalSessions, presentCount, excusedCount, presentPercentage, excusedPercentage }`. |
| GET | `/api/student/Details?id={guid}` | Admin/Manager | Student details inside authorized school scope. |
| POST | `/api/student/Create` | Admin/Manager | Creates the account/profile; antiforgery required. |
| GET | `/api/student/Edit?id={guid}` | Admin/Manager | Loads edit data inside school scope. |
| PUT | `/api/student/Edit` | Admin/Manager | Updates a student using JSON plus antiforgery. |
| DELETE | `/api/student/Delete` | Admin/Manager | Administrative/soft delete using `{ id }` plus antiforgery. |
| GET/POST | `/api/student/ChangeClass` | Admin/Manager | Loads and applies a class change in school scope. |

Chart endpoints reject another student's GUID even when it exists: `ValidateStudentDataAccessAsync` matches it to the authenticated Identity profile.

### 20.5 Teacher, manager, class, and subject APIs

| Group | Main routes | Purpose |
|---|---|---|
| Teacher | `/api/teacher/Create`, `/Details`, `/Edit`, `/Delete` | Teacher CRUD within role/school scope. |
| Assignments | `/api/teacher/AddTeacherToClassesAndLectuers`, `/RemoveTeacherToClassLectuers`, `/ManagerStudentToTeacher` | Teacher/class/subject/student assignment. |
| Teacher charts | `/api/teacher/grade-distribution`, `/api/teacher/attendance-summary` | Ownership-checked dashboard data. |
| Manager tables | `/api/menegar/MenegarStudent`, `/MenegarTeacher`, `/MenegarClass`, `/MenegarStudentInClass`, `/MenegarTeacherInClass` | School-scoped DataTables data. |
| Manager statistics | `/api/menegar/CountTeacherPerSubject` | Teacher count per subject. |
| Classes | `/api/theClass/GetClasses`, `/GetClassToStudent`, `/Create`, `/Edit`, `/CreateTeacherClass`, `/Delete` | Class lists, CRUD, and assignment. |
| Subjects | `/api/lectuer/GetLectuers`, `/LectuersData`, `/Create`, `/Edit`, `/TeacherLectuer`, `/StudentLectuer`, `/Delete`, `/DeleteTeacher` | Subject lists, CRUD, and links. |

Some legacy controllers contain actions without explicit route templates. New clients should use only explicit routes documented here, and every new endpoint should declare its template.

### 20.6 Exports, images, Redis, and certificates

| Method | Route | Description |
|---|---|---|
| GET | `/api/ExportDataApi/...` | Role-protected Excel exports; action and parameters select the export. |
| POST | Profile-image upload endpoints | `multipart/form-data`, antiforgery, extension/size checks, and safe filenames. |
| GET | `/api/diagnostics/redis` | Admin-only connectivity, key count, type, TTL, and size without sensitive values. |

PDF certificates are protected MVC downloads, not JSON APIs:

```text
GET /Teacher/DownloadTeacherCertificate?idTeacher={guid}
GET /Student/DownloadStudentCertificate?idStudent={guid}
```

Teacher certificates list all distinct subjects on one page. Both actions enforce profile ownership or manager-school scope.

### 20.7 Secure JavaScript example

```javascript
const token = document.querySelector(
  'input[name="__RequestVerificationToken"]'
).value;

const response = await fetch('/api/Grades/records/RECORD_GUID', {
  method: 'PUT',
  credentials: 'same-origin',
  headers: {
    'Content-Type': 'application/json',
    'RequestVerificationToken': token
  },
  body: JSON.stringify({
    firstMonth: 20, mid: 30, secondMonth: 20, activity: 10, final: 20
  })
});

if (!response.ok) {
  const error = await response.json().catch(() => ({}));
  throw new Error(error.message ?? 'Request failed');
}
```

### 20.8 Trust boundaries and maintenance

- A GUID is never authorization; every endpoint rechecks role, school, and ownership.
- Client `teacherId` and `studentid` values are matched to Identity/database scope.
- Never return stack traces, tokens, credentials, or connection strings.
- Update the Razor/JavaScript caller and this reference in the same change whenever a contract changes.
- Add integration tests for 200, 400, 401, 403, and 404, including cross-user access attempts.

## 21. Directorate layer

- `Directorate` owns multiple schools, and every school has a mandatory directorate relationship.
- `DirectorateManager` is distinct from the school `Manager`; unique constraints permit one manager profile and one Identity account per directorate.
- Directorate managers may create, edit, activate, and deactivate their schools. The API never accepts a replacement `DirectorateId`, while organized moves use `/Transfers` and `/api/transfers`.
- Deactivation uses `School.IsActive`; it does not delete data, and session validation rejects accounts in an inactive school.
- The dashboard exposes aggregate counts without personal records.
- Development-only load-test seeding creates two ministries and `directorate1`, `directorate2`, ... accounts, then distributes seeded schools in round-robin order. None of this data is stored in migrations.
- Shared subjects are Arabic, Mathematics, English, and Islamic Education; they remain school-scoped until a Ministry catalog is designed.

### 21.1 Directorate API

| Method and route | Purpose | Response |
|---|---|---|
| `GET /api/directorate/dashboard` | Directorate identity and statistics | Aggregate JSON object |
| `GET /api/directorate/schools` | Current directorate's schools | School array |
| `GET /api/directorate/active-schools` | Active schools in the current directorate | Active-school array with aggregate counts |
| `GET /api/directorate/managers` | School managers in the current directorate | School, contact, join-date, and account-state rows |
| `GET /api/directorate/teachers` | Teachers in the current directorate | School, contact, subject/class coverage, and account-state rows |
| `GET /api/directorate/students` | Students in the current directorate | School, class, contact, join-date, and account-state rows |
| `GET /api/directorate/classes` | Classes in the current directorate | School, stage, class metadata, student counts, and teacher counts |
| `GET /api/directorate/directory-options?schoolId={id}&personType={type}` | Active schools and owned-school classes; `manager` excludes schools that already have a manager | Create-form options |
| `POST /api/directorate/managers` | Create a school-manager profile in directorate scope | 201 and manager ID |
| `POST /api/directorate/teachers` | Create a teacher profile in directorate scope | 201 and teacher ID |
| `POST /api/directorate/students` | Create a student and current class-assignment links | 201 and student ID |
| `GET /api/directorate/schools/{id}` | Read an owned school | School object or 403 |
| `GET /api/directorate/schools/{id}/report` | Aggregate operational and academic report for an owned school | Profile, metrics, attendance, grades, classes, and subjects, or 403 |
| `GET /api/directorate/school-options` | Status, gender, and stage choices | Options object |
| `POST /api/directorate/schools` | Create a school | 201 and school ID |
| `PUT /api/directorate/schools/{id}` | Edit without transferring | 204 |
| `PATCH /api/directorate/schools/{id}/activation` | Activate/deactivate without deletion | 204 |

Write endpoints enforce Identity, role, ownership, and CSRF; client-supplied directorate identifiers are never trusted. Read routes also re-check school ownership; possession of a valid GUID is not authorization.

### 21.2 MVC routes and user experience

| Route | Purpose |
|---|---|
| `GET /Directorate` | Accessible linked statistic cards with hover/focus motion for each directorate directory. |
| `GET /Directorate/Schools` | Searchable, pageable school table with edit, activation, and report actions. |
| `GET /Directorate/ActiveSchools` | Active-school list with aggregate indicators and report links. |
| `GET /Directorate/Managers` | School-manager directory and account states. |
| `GET /Directorate/Teachers` | Teacher directory with subject and class coverage. |
| `GET /Directorate/Students` | Student directory with school, class, and account state. |
| `GET /Directorate/Classes` | Class distribution with student and teacher counts. |
| `GET /Directorate/CreateManager` | Complete owned-school manager profile form. |
| `GET /Directorate/CreateTeacher` | Complete owned-school teacher profile form. |
| `GET /Directorate/CreateStudent` | Complete student form with owned school and class selection. |
| `GET /Directorate/CreateSchool` | Create-school form. |
| `GET /Directorate/EditSchool/{id}` | Edit an owned school. |
| `GET /Directorate/SchoolDetails/{id}` | Printable aggregate school report. |

The school table uses client-side DataTables over `GET /api/directorate/schools`. It defaults to 10 rows, offers 5/10/25/50 page sizes, and preserves the current page after an activation change. DataTables `scrollX`, an `overflow-x: auto` wrapper, and a minimum table width keep columns and action buttons readable on narrow screens.

Directorate create forms add the complete operational profile after school/class ownership, age, identity-number, and email checks. Username/password creation and Identity role linkage remain in the existing `Account/SetCredentials` flow keyed by identity number; directorate forms never store passwords.

### 21.3 School report contract

`SchoolDetails.cshtml` consumes one endpoint:

```text
GET /api/directorate/schools/{schoolId}/report
```

The JSON response contains:

- `school`: name, activation, official status, gender, stage, and minimum/maximum class.
- `summary`: active manager, teacher, student, class, and subject counts.
- `attendance`: total records, present (`1`), absent (`0`), excused (`m`), and `attendanceRate`. The rate is `(present + excused) / total`, or `null` when no records exist.
- `academic`: grade-record count, average `Total`, and `distribution` buckets: below 50, 50-59, 60-69, 70-79, 80-89, and 90-100. Each bucket returns `label`, `count`, and `percentage`.
- `classes`: class name, stage, number, section, branch, and non-deleted student count.
- `subjects`: subject name plus distinct active teacher and class assignment counts derived from `TeacherLectuerClass`.
- `generatedAt`: UTC report-generation timestamp.

The report intentionally excludes student/teacher names, identity numbers, and contact details. Both MVC and API routes call `ValidateDirectorateSchoolAccessAsync`.

The UI provides loading, error, and empty states; horizontally scrollable report tables; and a print button bound through JavaScript to `window.print()`. Print styles hide navigation/actions and remove unnecessary shadows.

### 21.4 Directorate directory pages

The five detailed directory routes share an RTL DataTables view with search, page-size selection, sorting, pagination, empty states, conditional horizontal scrolling, and project-standard Notyf errors. Dashboard cards are full links and support both pointer hover and keyboard focus. Every API derives the directorate from the authenticated manager session and never trusts a client-supplied directorate identifier.

### 21.5 Reuse in other clients

The Razor layer is an API client for `/api/directorate/*`, so the contracts can support another web or mobile client. The endpoint is not public: a new client must provide the configured authentication context and adapt Identity/Session and CSRF behavior while preserving server-side role and ownership checks. Contract changes must update the client, this reference, and integration tests together.
## 22. Ministry layer

The existing `Admin` role currently represents the Ministry layer to preserve compatibility with existing accounts and authorization rules. This layer is system-wide and supervisory; it does not grant Ministry users the daily operational routes assigned to directorate or school managers.

### 22.1 Ministry UI routes

| Route | Purpose |
|---|---|
| `GET /Ministry` | National aggregate dashboard. |
| `GET /Ministry/Directorates` | Searchable, sortable, pageable directorate directory. |
| `GET /Ministry/DirectorateDetails/{id}` | Directorate profile, manager, aggregates, and owned schools. |

The pages use RTL layout, shared project components, and project-standard Notyf notifications. The directorate table scrolls horizontally only when its content actually overflows, and linked cards provide visible hover and keyboard-focus states.

### 22.2 API contracts

| Method and route | Purpose |
|---|---|
| `GET /api/ministry/dashboard` | Aggregate directorate, school, manager, teacher, student, and class metrics. |
| `GET /api/ministry/directorates` | Directorates with their managers and aggregate counts. |
| `GET /api/ministry/directorates/{id}` | One directorate report and its schools. |
| `PATCH /api/ministry/directorates/{id}/activation` | Activate or deactivate a directorate without deletion. |

All Ministry MVC and API routes require `Admin`. Deactivation preserves all records and causes the existing directorate-session validation to reject a directorate manager when `Directorate.IsActive` is false. Ministry pages do not reuse the school report protected for `DirectorateManager`; a Ministry-specific school report is deferred to its own phase so authorization boundaries remain explicit.
## 23. Organization hierarchy and transfers

The organizational hierarchy is now `Ministry -> Directorate -> School`. The current local development database contains two ministries (`MIN-01`, `MIN-02`) and two directorates (`LOAD-DIR-01`, `LOAD-DIR-02`); these are development data and are not part of production migrations. The empty `LEGACY` directorate was removed after an emptiness check. Load-test schools 1 and 3 belong to the first directorate, school 2 belongs to the second, and the existing Yibna boys school remains in the first directorate.

`TeacherPlacement` supports multiple school placements with one active primary placement, `SchoolManagerAssignment` supports multi-school management, and `StudentEnrollment` enforces one active student enrollment. The `BackfillOrganizationAssignments` migration preserved and migrated existing data: 90 teacher placements, 7 school-manager assignments, and 3,001 student enrollments.

Transfer requests identify teachers, students, and school managers by identity number. Transfers inside one directorate complete immediately; cross-directorate moves create a request for the source directorate. Approval closes the previous primary association, creates the destination association, updates compatibility fields, and reactivates the Identity account when needed.

| Route | Purpose |
|---|---|
| `GET /Transfers` | Create and manage transfer requests. |
| `GET /api/transfers/options` | Schools and classes allowed for the current authority. |
| `GET /api/transfers?direction=incoming|outgoing` | Incoming or outgoing requests. |
| `POST /api/transfers` | Create a request by identity number and destination school. |
| `PATCH /api/transfers/{id}/decision` | Approve/reject and execute an approved transfer. |
| `GET /api/ministry/ministries` | Ministries with organization and person aggregates. |

Re-adding a disabled teacher through the directorate flow now reactivates the same profile and account and creates a new primary placement instead of duplicating the identity. New manager, teacher, and student creation writes both the new association tables and the compatibility fields during the transition period.
## 24. Production data and seeding policy

Migrations are structural only and do not add sample ministries, directorates, schools, people, or transfer requests. The full migration chain was tested against a clean temporary LocalDB database; `Ministry`, `Directorate`, `School`, `Teacher`, `Student`, `Menegar`, and `TransferRequest` all contained zero rows afterward.

`LoadTestDataSeeder` is protected by two conditions: the environment must be `Development` and `LoadTestSeed:Enabled` must be `true`. It therefore cannot run in Production even if configuration is added accidentally. `IdentityDataSeeder.SeedMainAdminAsync` is the only production seed. It runs only when `SeedAdmin:Enabled=true` and the official account values and password are supplied through secure deployment configuration, environment variables, or a secret store. The administrator password is never committed to the repository.

Application initialization applies migrations, ensures required roles, and creates or updates only the configured official administrator. The legacy migration creates `LEGACY` only while upgrading a database that already contains schools; a clean production database receives no transitional row.
