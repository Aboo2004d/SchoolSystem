using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models;

public class DirectorateSchoolRequest
{
    [Required, StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    public Guid? IdStatusSchool { get; set; }
    public Guid? IdGender { get; set; }
    public Guid? IdStage { get; set; }
    [Range(1, 12)] public int? MinClass { get; set; }
    [Range(1, 12)] public int? MaxClass { get; set; }
}

public sealed class SchoolActivationRequest
{
    public bool IsActive { get; set; }
}
