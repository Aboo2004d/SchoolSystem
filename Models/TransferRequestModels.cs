using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models;

public sealed class CreateTransferRequest
{
    [Required]
    public string SubjectType { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int IdentityNumber { get; set; }

    [Required]
    public Guid DestinationSchoolId { get; set; }

    public Guid? DestinationClassId { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }
}

public sealed class DecideTransferRequest
{
    public bool Approve { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }
}
