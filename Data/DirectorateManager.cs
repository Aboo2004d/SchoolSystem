namespace SchoolSystem.Data;

public sealed class DirectorateManager
{
    public Guid Id { get; set; }
    public Guid DirectorateId { get; set; }
    public Guid? ApplicationUserId { get; set; }
    public string Name { get; set; } = null!;
    public int? IdNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? Area { get; set; }
    public bool IsDeleted { get; set; }
    public Directorate Directorate { get; set; } = null!;
    public ApplicationUser? ApplicationUser { get; set; }
}
