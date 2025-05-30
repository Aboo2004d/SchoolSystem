using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class Acount
{
    public int Id { get; set; }

    public string UsersName { get; set; } = null!;

    public string? Passwords { get; set; }

    public string? Email { get; set; }

    public string? Role { get; set; }

    public string ResetToken { get; set; } = null!;

    public DateTime ResetTokenExpiry { get; set; }

    public int? IdUser { get; set; }

    public bool? IsActive { get; set; }
}
