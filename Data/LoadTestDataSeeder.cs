using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SchoolSystem.Data;

public sealed class LoadTestSeedOptions
{
    public bool Enabled { get; set; }
    public int Schools { get; set; } = 3;
    public int Directorates { get; set; } = 2;
    public int ManagersPerSchool { get; set; } = 2;
    public int TeachersPerSchool { get; set; } = 30;
    public int ClassesPerSchool { get; set; } = 12;
    public int SubjectsPerSchool { get; set; } = 8;
    public int StudentsPerSchool { get; set; } = 1000;
    public int AttendanceDays { get; set; } = 5;
    public string Password { get; set; } = string.Empty;
}

public static class LoadTestDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var options = configuration.GetSection("LoadTestSeed").Get<LoadTestSeedOptions>();
        if (options is null || !options.Enabled) return;
        if (string.IsNullOrWhiteSpace(options.Password))
            throw new InvalidOperationException("Set LoadTestSeed:Password in User Secrets.");

        static void Progress(string message) =>
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [LOAD-SEED] {message}");

        if (options.Directorates < 1)
            throw new InvalidOperationException("LoadTestSeed:Directorates must be at least 1.");
        Progress($"Starting: {options.Directorates} directorates, {options.Schools} schools, {options.StudentsPerSchool} students/school.");
        var db = services.GetRequiredService<SystemSchoolDbContext>();
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

        var roles = await db.Roles.Where(x => x.Name == RoleNames.DirectorateManager || x.Name == RoleNames.Manager || x.Name == RoleNames.Teacher || x.Name == RoleNames.Student)
            .ToDictionaryAsync(x => x.Name!, x => x.Id, cancellationToken);
        var hasher = services.GetRequiredService<IPasswordHasher<ApplicationUser>>();
        var normalizer = services.GetRequiredService<ILookupNormalizer>();
        var passwordTemplate = new ApplicationUser();
        var sharedHash = hasher.HashPassword(passwordTemplate, options.Password);

        var directorates = new List<Directorate>(options.Directorates);
        for (var index = 1; index <= options.Directorates; index++)
        {
            var code = $"LOAD-DIR-{index:00}";
            var directorate = await db.Directorates.SingleOrDefaultAsync(x => x.Code == code, cancellationToken);
            if (directorate is null)
            {
                directorate = new Directorate
                {
                    Id = Guid.NewGuid(), Code = code, Name = $"مديرية الاختبار {index}",
                    City = $"مدينة {index}", Area = $"منطقة {index}", IsActive = true
                };
                db.Directorates.Add(directorate);
            }
            else
            {
                directorate.IsActive = true;
            }
            directorates.Add(directorate);
        }
        await db.SaveChangesAsync(cancellationToken);

        for (var index = 1; index <= directorates.Count; index++)
        {
            var directorate = directorates[index - 1];
            var existingProfile = await db.DirectorateManagers
                .SingleOrDefaultAsync(x => x.DirectorateId == directorate.Id, cancellationToken);
            if (existingProfile is not null) continue;

            var userName = $"directorate{index}";
            var user = await db.Users.SingleOrDefaultAsync(x => x.UserName == userName, cancellationToken);
            user ??= MakeUser(userName, $"{userName}@loadtest.local", sharedHash, normalizer);
            if (db.Entry(user).State == EntityState.Detached) db.Users.Add(user);
            if (!await db.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == roles[RoleNames.DirectorateManager], cancellationToken))
                db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = roles[RoleNames.DirectorateManager] });
            db.DirectorateManagers.Add(new DirectorateManager
            {
                Id = Guid.NewGuid(), DirectorateId = directorate.Id, ApplicationUserId = user.Id,
                Name = $"مسؤول المديرية {index}", Email = user.Email, Phone = $"058{index:0000000}",
                IdNumber = 400000000 + index
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
        Progress("Directorates and their Identity manager accounts are ready.");

        var status = await db.StatusSchools.FirstOrDefaultAsync(x => x.TheType == "Active", cancellationToken)
            ?? new StatusSchool { Id = Guid.NewGuid(), Condition = true, TheType = "Active" };
        var gender = await db.Genders.FirstOrDefaultAsync(x => x.TheType == "Mixed", cancellationToken)
            ?? new Gender { Id = Guid.NewGuid(), TheType = "Mixed" };
        var stage = await db.StageClasses.FirstOrDefaultAsync(x => x.Code == "L", cancellationToken)
            ?? new StageClass { Id = Guid.NewGuid(), Code = "L", MinClass = 1, MaxClass = 12, NameStage = "Load Test" };
        var branch = await db.Branches.FirstOrDefaultAsync(x => x.BranchCode == "L", cancellationToken)
            ?? new Branch { Id = Guid.NewGuid(), BranchCode = "L", BranchName = "Load Test" };
        if (db.Entry(status).State == EntityState.Detached) db.Add(status);
        if (db.Entry(gender).State == EntityState.Detached) db.Add(gender);
        if (db.Entry(stage).State == EntityState.Detached) db.Add(stage);
        if (db.Entry(branch).State == EntityState.Detached) db.Add(branch);
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(cancellationToken);
        Progress("Reference data is ready.");

        var managerCounter = await db.Users.CountAsync(x => x.UserName != null && x.UserName.StartsWith("manager"), cancellationToken);
        var teacherCounter = await db.Users.CountAsync(x => x.UserName != null && x.UserName.StartsWith("teacher"), cancellationToken);
        var studentCounter = await db.Users.CountAsync(x => x.UserName != null && x.UserName.StartsWith("stu"), cancellationToken);
        for (var schoolIndex = 1; schoolIndex <= options.Schools; schoolIndex++)
        {
            var schoolName = $"LoadTest School {schoolIndex}";
            var directorateId = directorates[(schoolIndex - 1) % directorates.Count].Id;
            var completeSchool = await db.Schools.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Name == schoolName && !x.IsDeleted, cancellationToken);
            if (completeSchool is not null)
            {
                await db.Schools.Where(x => x.Id == completeSchool.Id).ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.DirectorateId, directorateId).SetProperty(x => x.IsActive, true), cancellationToken);
                await ApplyCommonSubjectNamesAsync(db, completeSchool.Id, cancellationToken);
                Progress($"School {schoolIndex}/{options.Schools} already complete; skipped.");
                continue;
            }
            var incompleteSchool = await db.Schools.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Name == schoolName && x.IsDeleted, cancellationToken);
            if (incompleteSchool is not null)
            {
                await db.Schools.Where(x => x.Id == incompleteSchool.Id).ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.DirectorateId, directorateId).SetProperty(x => x.IsActive, true), cancellationToken);
                await ApplyCommonSubjectNamesAsync(db, incompleteSchool.Id, cancellationToken);
                Progress($"School {schoolIndex}/{options.Schools} is incomplete; resuming missing rows...");
                await ResumeIncompleteSchoolAsync(db, incompleteSchool, options, schoolIndex, Progress, cancellationToken);
                continue;
            }
            Progress($"School {schoolIndex}/{options.Schools}: creating core data and accounts...");
            var school = new School
            {
                Id = Guid.NewGuid(), Name = schoolName, IdStatusSchool = status.Id,
                DirectorateId = directorateId, IsActive = true,
                IdGender = gender.Id, IdStage = stage.Id, MinClass = 1, MaxClass = 12, IsDeleted = true
            };
            db.Schools.Add(school);

            var classes = Enumerable.Range(1, options.ClassesPerSchool).Select(i => new TheClass
            {
                Id = Guid.NewGuid(), Name = $"Class {i}", IdSchool = school.Id, IdStage = stage.Id,
                IdBranch = branch.Id, NumberClass = ((i - 1) % 12) + 1, Section = ((i - 1) / 12) + 1
            }).ToArray();
            var commonSubjects = new[] { "اللغة العربية", "الرياضيات", "اللغة الإنجليزية", "التربية الإسلامية" };
            var subjects = Enumerable.Range(1, Math.Max(options.SubjectsPerSchool, commonSubjects.Length)).Select(i => new Lectuer
            {
                Id = Guid.NewGuid(), Name = i <= commonSubjects.Length ? commonSubjects[i - 1] : $"مادة تجريبية {i}", IdSchool = school.Id
            }).ToArray();
            db.AddRange(classes);
            db.AddRange(subjects);

            for (var i = 0; i < options.ManagersPerSchool; i++)
            {
                var number = ++managerCounter;
                var user = MakeUser($"manager{number}", $"manager{number}@loadtest.local", sharedHash, normalizer);
                db.Users.Add(user);
                db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = roles[RoleNames.Manager] });
                db.Menegars.Add(new Menegar { Id = Guid.NewGuid(), ApplicationUserId = user.Id, IdSchool = school.Id,
                    Name = $"Manager {number}", Email = user.Email, Phone = $"056{number:0000000}", IdNumber = 500000000 + number });
            }

            var teachers = new List<Teacher>(options.TeachersPerSchool);
            for (var i = 0; i < options.TeachersPerSchool; i++)
            {
                var number = ++teacherCounter;
                var user = MakeUser($"teacher{number}", $"teacher{number}@loadtest.local", sharedHash, normalizer);
                var teacher = new Teacher { Id = Guid.NewGuid(), ApplicationUserId = user.Id, IdSchool = school.Id,
                    Name = $"Teacher {number}", Email = user.Email, Phone = $"057{number:0000000}", IdNumber = 600000000 + number };
                teachers.Add(teacher); db.Users.Add(user); db.Teachers.Add(teacher);
                db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = roles[RoleNames.Teacher] });
            }

            var assignments = new List<TeacherLectuerClass>();
            for (var c = 0; c < classes.Length; c++)
                for (var s = 0; s < subjects.Length; s++)
                    assignments.Add(new TeacherLectuerClass { Id = Guid.NewGuid(), IdSchool = school.Id, IdClass = classes[c].Id,
                        IdLectuer = subjects[s].Id, IdTeacher = teachers[(c * subjects.Length + s) % teachers.Count].Id });
            db.TeacherLectuerClasses.AddRange(assignments);

            var students = new List<Student>(options.StudentsPerSchool);
            for (var i = 0; i < options.StudentsPerSchool; i++)
            {
                var number = ++studentCounter;
                var user = MakeUser($"stu{number}", $"stu{number}@loadtest.local", sharedHash, normalizer);
                var student = new Student { Id = Guid.NewGuid(), ApplicationUserId = user.Id, IdSchool = school.Id,
                    IdClass = classes[i % classes.Length].Id, Name = $"Student {number}", Email = user.Email,
                    Phone = $"059{number:0000000}", IdNumber = 700000000 + number };
                students.Add(student); db.Users.Add(user); db.Students.Add(student);
                db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = roles[RoleNames.Student] });
            }

            // Persist core entities first; EF still batches INSERT statements, but the change tracker remains bounded.
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
            Progress($"School {schoolIndex}: core saved ({options.ManagersPerSchool} managers, {options.TeachersPerSchool} teachers, {options.StudentsPerSchool} students).");

            var links = new List<StudentLectuerTeacher>(students.Count * subjects.Length);
            var grades = new List<Grade>(students.Count * subjects.Length);
            var attendance = new List<Attendance>(students.Count * subjects.Length * options.AttendanceDays);
            foreach (var student in students)
            {
                var classIndex = Array.FindIndex(classes, x => x.Id == student.IdClass);
                for (var s = 0; s < subjects.Length; s++)
                {
                    var teacher = teachers[(classIndex * subjects.Length + s) % teachers.Count];
                    links.Add(new StudentLectuerTeacher { Id = Guid.NewGuid(), IdSchool = school.Id, IdClass = student.IdClass,
                        IdLectuer = subjects[s].Id, IdTeacher = teacher.Id, IdStudent = student.Id });
                    grades.Add(new Grade { GradesId = Guid.NewGuid(), IdSchool = school.Id, IdClass = student.IdClass,
                        IdLectuer = subjects[s].Id, IdTeacher = teacher.Id, IdStudent = student.Id,
                        FirstMonth = 15 + numberMod(student.Id, 6), Mid = 20, SecondMonth = 15, Activity = 10, Final = 30 });
                    for (var day = 0; day < options.AttendanceDays; day++)
                        attendance.Add(new Attendance { Id = Guid.NewGuid(), IdSchool = school.Id, IdClass = student.IdClass,
                            IdLectuer = subjects[s].Id, IdTeacher = teacher.Id, IdStudent = student.Id,
                            DateAndTime = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-day)), AttendanceStatus = day % 9 == 0 ? "0" : "1" });
                }
            }
            var linkBatches = links.Chunk(500).ToArray();
            for (var batchIndex = 0; batchIndex < linkBatches.Length; batchIndex++)
            {
                db.StudentLectuerTeachers.AddRange(linkBatches[batchIndex]);
                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
                Progress($"School {schoolIndex}: links batch {batchIndex + 1}/{linkBatches.Length}.");
            }
            var gradeBatches = grades.Chunk(500).ToArray();
            for (var batchIndex = 0; batchIndex < gradeBatches.Length; batchIndex++)
            {
                db.Grades.AddRange(gradeBatches[batchIndex]);
                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
                Progress($"School {schoolIndex}: grades batch {batchIndex + 1}/{gradeBatches.Length}.");
            }

            var attendanceBatches = attendance.Chunk(250).ToArray();
            for (var batchIndex = 0; batchIndex < attendanceBatches.Length; batchIndex++)
            {
                db.Attendances.AddRange(attendanceBatches[batchIndex]);
                await db.SaveChangesAsync(cancellationToken);
                db.ChangeTracker.Clear();
                Progress($"School {schoolIndex}: attendance batch {batchIndex + 1}/{attendanceBatches.Length}.");
            }
            school.IsDeleted = false;
            db.Schools.Update(school);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
            Progress($"School {schoolIndex}/{options.Schools} committed successfully.");
        }
        Progress("Load-test seed completed successfully.");
    }

    private static async Task ApplyCommonSubjectNamesAsync(SystemSchoolDbContext db, Guid schoolId,
        CancellationToken cancellationToken)
    {
        var names = new[] { "اللغة العربية", "الرياضيات", "اللغة الإنجليزية", "التربية الإسلامية" };
        var subjects = await db.Lectuers.Where(x => x.IdSchool == schoolId).OrderBy(x => x.Name)
            .Take(names.Length).ToListAsync(cancellationToken);
        for (var i = 0; i < subjects.Count; i++) subjects[i].Name = names[i];
        if (subjects.Count > 0) await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
    }

    private static async Task ResumeIncompleteSchoolAsync(SystemSchoolDbContext db, School school,
        LoadTestSeedOptions options, int schoolIndex, Action<string> progress, CancellationToken cancellationToken)
    {
        var classes = await db.TheClasses.AsNoTracking().Where(x => x.IdSchool == school.Id)
            .OrderBy(x => x.NumberClass).ThenBy(x => x.Section).ToArrayAsync(cancellationToken);
        var subjects = await db.Lectuers.AsNoTracking().Where(x => x.IdSchool == school.Id)
            .OrderBy(x => x.Name).ToArrayAsync(cancellationToken);
        var students = await db.Students.AsNoTracking().Where(x => x.IdSchool == school.Id)
            .ToArrayAsync(cancellationToken);
        var assignments = await db.TeacherLectuerClasses.AsNoTracking().Where(x => x.IdSchool == school.Id)
            .Select(x => new { x.IdClass, x.IdLectuer, x.IdTeacher }).ToArrayAsync(cancellationToken);

        if (classes.Length == 0 || subjects.Length == 0 || students.Length == 0 || assignments.Length == 0)
            throw new InvalidOperationException($"Incomplete school {school.Name} has missing core data and cannot be resumed safely.");

        var teacherByClassSubject = assignments.ToDictionary(x => (x.IdClass, x.IdLectuer), x => x.IdTeacher);
        var existingLinksRaw = await db.StudentLectuerTeachers.AsNoTracking().Where(x => x.IdSchool == school.Id)
            .Select(x => new { x.IdStudent, x.IdLectuer }).ToArrayAsync(cancellationToken);
        var existingLinks = existingLinksRaw.Select(x => (x.IdStudent, x.IdLectuer)).ToHashSet();
        var existingGradesRaw = await db.Grades.AsNoTracking().Where(x => x.IdSchool == school.Id)
            .Select(x => new { x.IdStudent, x.IdLectuer }).ToArrayAsync(cancellationToken);
        var existingGrades = existingGradesRaw.Select(x => (x.IdStudent, x.IdLectuer)).ToHashSet();
        var existingAttendanceRaw = await db.Attendances.AsNoTracking().Where(x => x.IdSchool == school.Id)
            .Select(x => new { x.IdStudent, x.IdLectuer, x.DateAndTime }).ToArrayAsync(cancellationToken);
        var existingAttendance = existingAttendanceRaw.Select(x => (x.IdStudent, x.IdLectuer, x.DateAndTime)).ToHashSet();

        var missingLinks = new List<StudentLectuerTeacher>();
        var missingGrades = new List<Grade>();
        var missingAttendance = new List<Attendance>();
        var seedDate = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        foreach (var student in students)
        foreach (var subject in subjects)
        {
            if (!teacherByClassSubject.TryGetValue((student.IdClass, subject.Id), out var teacherId))
                throw new InvalidOperationException($"Missing teacher assignment for class {student.IdClass} and subject {subject.Id}.");

            if (!existingLinks.Contains((student.Id, subject.Id)))
                missingLinks.Add(new StudentLectuerTeacher { Id = Guid.NewGuid(), IdSchool = school.Id,
                    IdClass = student.IdClass, IdLectuer = subject.Id, IdTeacher = teacherId, IdStudent = student.Id });
            if (!existingGrades.Contains((student.Id, subject.Id)))
                missingGrades.Add(new Grade { GradesId = Guid.NewGuid(), IdSchool = school.Id,
                    IdClass = student.IdClass, IdLectuer = subject.Id, IdTeacher = teacherId, IdStudent = student.Id,
                    FirstMonth = 15 + numberMod(student.Id, 6), Mid = 20, SecondMonth = 15, Activity = 10, Final = 30 });
            for (var day = 0; day < options.AttendanceDays; day++)
            {
                var date = seedDate.AddDays(-day);
                if (!existingAttendance.Contains((student.Id, subject.Id, date)))
                    missingAttendance.Add(new Attendance { Id = Guid.NewGuid(), IdSchool = school.Id,
                        IdClass = student.IdClass, IdLectuer = subject.Id, IdTeacher = teacherId, IdStudent = student.Id,
                        DateAndTime = date, AttendanceStatus = day % 9 == 0 ? "0" : "1" });
            }
        }

        progress($"School {schoolIndex}: found {missingLinks.Count} missing links, {missingGrades.Count} grades, {missingAttendance.Count} attendance rows.");
        await SaveResumeBatchesAsync(db, missingLinks, 500, "links", schoolIndex, progress, cancellationToken);
        await SaveResumeBatchesAsync(db, missingGrades, 500, "grades", schoolIndex, progress, cancellationToken);
        await SaveResumeBatchesAsync(db, missingAttendance, 250, "attendance", schoolIndex, progress, cancellationToken);

        school.IsDeleted = false;
        db.Schools.Update(school);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
        progress($"School {schoolIndex} resumed and committed successfully.");
    }

    private static async Task SaveResumeBatchesAsync<TEntity>(SystemSchoolDbContext db, List<TEntity> rows,
        int batchSize, string label, int schoolIndex, Action<string> progress, CancellationToken cancellationToken)
        where TEntity : class
    {
        var batches = rows.Chunk(batchSize).ToArray();
        for (var index = 0; index < batches.Length; index++)
        {
            db.Set<TEntity>().AddRange(batches[index]);
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
            progress($"School {schoolIndex}: resumed {label} batch {index + 1}/{batches.Length}.");
        }
    }

    private static int numberMod(Guid id, int divisor) => Math.Abs(BitConverter.ToInt32(id.ToByteArray(), 0) % divisor);

    private static ApplicationUser MakeUser(string userName, string email, string passwordHash, ILookupNormalizer normalizer) => new()
    {
        Id = Guid.NewGuid(), UserName = userName, NormalizedUserName = normalizer.NormalizeName(userName),
        Email = email, NormalizedEmail = normalizer.NormalizeEmail(email), EmailConfirmed = true, IsActive = true,
        PasswordHash = passwordHash, SecurityStamp = Guid.NewGuid().ToString("N"), ConcurrencyStamp = Guid.NewGuid().ToString("N")
    };
}
