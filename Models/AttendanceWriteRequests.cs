using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models;

public sealed class AttendanceBatchRequest
{
    public Guid TeacherId { get; set; }
    public Guid LectuerId { get; set; }
    public Guid ClassId { get; set; }

    [Required, MinLength(1), MaxLength(500)]
    public List<AttendanceItemRequest> Items { get; set; } = [];
}

public sealed class AttendanceItemRequest
{
    public Guid StudentId { get; set; }

    [Required, RegularExpression("^(1|0|m)$")]
    public string Status { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Excuse { get; set; }
}

public sealed class AttendanceUpdateRequest
{
    [Required, RegularExpression("^(1|0|m)$")]
    public string Status { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Excuse { get; set; }
}
