using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class Menegar
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Phone { get; set; }

    public string? Email { get; set; }
}
