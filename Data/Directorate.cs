namespace SchoolSystem.Data;

public sealed class Directorate
{
    public Guid Id { get; set; }
    public Guid MinistryId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? City { get; set; }
    public string? Area { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<School> Schools { get; set; } = new List<School>();
    public DirectorateManager? Manager { get; set; }
    public Ministry Ministry { get; set; } = null!;
}
