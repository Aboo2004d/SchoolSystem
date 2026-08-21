namespace SchoolSystem.Data;

public sealed class Ministry
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<Directorate> Directorates { get; set; } = new List<Directorate>();
    public MinistryManager? Manager { get; set; }
}

public sealed class MinistryManager
{
    public Guid Id { get; set; }
    public Guid MinistryId { get; set; }
    public Guid? ApplicationUserId { get; set; }
    public string Name { get; set; } = null!;
    public int? IdNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsDeleted { get; set; }
    public Ministry Ministry { get; set; } = null!;
    public ApplicationUser? ApplicationUser { get; set; }
}

public sealed class TeacherPlacement
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public Guid SchoolId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public Teacher Teacher { get; set; } = null!;
    public School School { get; set; } = null!;
}

public sealed class SchoolManagerAssignment
{
    public Guid Id { get; set; }
    public Guid ManagerId { get; set; }
    public Guid SchoolId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public Menegar Manager { get; set; } = null!;
    public School School { get; set; } = null!;
}

public sealed class StudentEnrollment
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid SchoolId { get; set; }
    public Guid ClassId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public Student Student { get; set; } = null!;
    public School School { get; set; } = null!;
    public TheClass Class { get; set; } = null!;
}

public sealed class TransferRequest
{
    public Guid Id { get; set; }
    public string SubjectType { get; set; } = null!;
    public Guid SubjectId { get; set; }
    public int SubjectIdentityNumber { get; set; }
    public string Status { get; set; } = TransferStatuses.PendingSourceApproval;
    public Guid? SourceMinistryId { get; set; }
    public Guid? DestinationMinistryId { get; set; }
    public Guid? SourceDirectorateId { get; set; }
    public Guid? DestinationDirectorateId { get; set; }
    public Guid? SourceSchoolId { get; set; }
    public Guid? DestinationSchoolId { get; set; }
    public Guid? SourceClassId { get; set; }
    public Guid? DestinationClassId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid? DecidedByUserId { get; set; }
    public string? Reason { get; set; }
    public string? DecisionNote { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public static class TransferSubjectTypes
{
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public const string SchoolManager = "SchoolManager";
}

public static class TransferStatuses
{
    public const string PendingSourceApproval = "PendingSourceApproval";
    public const string PendingDestinationApproval = "PendingDestinationApproval";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";
    public const string Completed = "Completed";
}

public static class SchoolOwnershipTypes
{
    public const string Government = "Government";
    public const string Private = "Private";
    public const string Agency = "Agency";
}

public static class SchoolEducationLevels
{
    public const string Primary = "Primary";
    public const string Preparatory = "Preparatory";
    public const string Secondary = "Secondary";
}
