using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models;

public sealed class GradeBatchRequest
{
    public Guid TeacherId { get; set; }
    public Guid LectuerId { get; set; }
    public Guid ClassId { get; set; }

    [Required, MinLength(1), MaxLength(500)]
    public List<GradeItemRequest> Items { get; set; } = [];
}

public sealed class GradeItemRequest
{
    public Guid StudentId { get; set; }
    [Range(0, 100)] public int? FirstMonth { get; set; }
    [Range(0, 100)] public int? Mid { get; set; }
    [Range(0, 100)] public int? SecondMonth { get; set; }
    [Range(0, 100)] public int? Activity { get; set; }
    [Range(0, 100)] public int? Final { get; set; }
}

public sealed class GradeUpdateRequest
{
    [Range(0, 100)] public int? FirstMonth { get; set; }
    [Range(0, 100)] public int? Mid { get; set; }
    [Range(0, 100)] public int? SecondMonth { get; set; }
    [Range(0, 100)] public int? Activity { get; set; }
    [Range(0, 100)] public int? Final { get; set; }
}
